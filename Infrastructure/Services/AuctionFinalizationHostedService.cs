using Application.Events;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Portal.Data;
using System.Collections.Concurrent;

namespace Infrastructure.Services;

public class AuctionFinalizationHostedService : BackgroundService
{
    private readonly IDomainEventPublisher _publisher;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<AuctionFinalizationHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<(Guid SessionId, int PlayerId), bool> _processed = new();

    public AuctionFinalizationHostedService(IDomainEventPublisher publisher, IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<AuctionFinalizationHostedService> logger, IServiceScopeFactory scopeFactory)
    {
        _publisher = publisher;
        _dbFactory = dbFactory;
        _logger = logger;
        _scopeFactory = scopeFactory;
    _publisher.EventPublished += OnEvent;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    private void OnEvent(object? sender, object @event)
    {
        switch (@event)
        {
            case PlayerAssignedViaBidding assigned:
                _ = HandleAssignedAsync(assigned);
                break;
            case BiddingTimerExpired expired:
                _ = HandleExpiryAsync(expired);
                break;
        }
    }

    private async Task HandleAssignedAsync(PlayerAssignedViaBidding e)
    {
        var key = (e.SessionId, e.SerieAPlayerId);
        if (_processed.ContainsKey(key)) return;
        try
        {
            using var db = _dbFactory.CreateDbContext();
            var session = await db.AuctionSessions.FirstOrDefaultAsync(s => s.Id == e.SessionId);
            if (session is null) return;

            var participants = await db.AuctionParticipants
                .Where(p => p.SessionId == e.SessionId)
                .OrderBy(p => p.OrderIndex)
                .Select(p => p.TeamId)
                .ToListAsync();
            if (participants.Count == 0) return;

            var teams = await db.Teams
                .Where(t => participants.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id);

            if (!teams.TryGetValue(e.TeamId, out var winner)) return;

            var player = await db.SerieAPlayers.FirstOrDefaultAsync(p => p.Id == e.SerieAPlayerId);
            if (player is null) return;
            var role = MapRole(player.PlayerType);

            winner.Assign(role, e.Amount);
            var ownership = PlayerOwnership.Create(winner, player, Guid.NewGuid(), e.Amount);
            db.PlayerOwnerships.Add(ownership);

            session.AdvanceAfterRound(participants, teams);
            await db.SaveChangesAsync();
            // Notify clients about assignment
            using (var scope = _scopeFactory.CreateScope())
            {
                var rt = scope.ServiceProvider.GetRequiredService<Application.Services.IRealtimeNotificationService>();
                await rt.PlayerAssigned(e.SessionId, e.SerieAPlayerId, e.TeamId, e.Amount);
            }
            _processed[key] = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PlayerAssignedViaBidding for Session {SessionId} Player {PlayerId}", e.SessionId, e.SerieAPlayerId);
        }
    }

    private async Task HandleExpiryAsync(BiddingTimerExpired e)
    {
        var key = (e.SessionId, e.SerieAPlayerId);
        // Allow time for PlayerAssignedViaBidding to arrive (race with timer service)
    const int maxWaitMs = 50;
        int waited = 0;
        while (waited < maxWaitMs)
        {
            await Task.Delay(25);
            waited += 25;
            if (_processed.ContainsKey(key)) return;
        }

        try
        {
            using var db = _dbFactory.CreateDbContext();
            var session = await db.AuctionSessions.FirstOrDefaultAsync(s => s.Id == e.SessionId);
            if (session is null) return;
            var participants = await db.AuctionParticipants
                .Where(p => p.SessionId == e.SessionId)
                .OrderBy(p => p.OrderIndex)
                .Select(p => p.TeamId)
                .ToListAsync();
            if (participants.Count == 0) return;
            var teams = await db.Teams
                .Where(t => participants.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id);

            // If assignment event wasn't processed, read persisted winning state from mapped properties
            var winningTeamId = session.CurrentHighestTeamId;
            var winningAmount = session.CurrentHighestBid;
            if (winningTeamId != Guid.Empty && winningAmount > 0)
            {
                if (teams.TryGetValue(winningTeamId, out var winner2))
                {
                    var player2 = await db.SerieAPlayers.FirstOrDefaultAsync(p => p.Id == e.SerieAPlayerId);
                    if (player2 is not null)
                    {
                        var role2 = MapRole(player2.PlayerType);
                        winner2.Assign(role2, winningAmount);
                        var ownership2 = PlayerOwnership.Create(winner2, player2, Guid.NewGuid(), winningAmount);
                        db.PlayerOwnerships.Add(ownership2);
                    }
                }
            }
            session.AdvanceAfterRound(participants, teams);
            await db.SaveChangesAsync();
            _processed[key] = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BiddingTimerExpired for Session {SessionId} Player {PlayerId}", e.SessionId, e.SerieAPlayerId);
        }
    }

    private static RoleType MapRole(PlayerType type) => type switch
    {
        PlayerType.Goalkeeper => RoleType.P,
        PlayerType.Defender => RoleType.D,
        PlayerType.Midfielder => RoleType.C,
        PlayerType.Forward => RoleType.A,
        _ => RoleType.P
    };
}
