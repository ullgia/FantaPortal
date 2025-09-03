using Application.Events;
using Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace Application.Services;

public class AuctionTimerHostedService : BackgroundService
{
    private readonly IDomainEventPublisher _publisher;
    private readonly IAuctionTimerManager _timerManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<(Guid SessionId, int PlayerId), Guid> _turnMap = new();
    private readonly ConcurrentDictionary<(Guid SessionId, int PlayerId), (Guid TeamId, int Amount)> _highestBids = new();

    public AuctionTimerHostedService(IDomainEventPublisher publisher, IAuctionTimerManager timerManager, IServiceScopeFactory scopeFactory)
    {
        _publisher = publisher;
        _timerManager = timerManager;
        _scopeFactory = scopeFactory;
    _publisher.EventPublished += OnEvent;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    private void OnEvent(object? sender, object @event)
    {
        if (@event is BiddingReadyCompleted ready)
        {
            // Start a timer for the bidding round; using playerId as a deterministic turn id surrogate for demo
            var key = (ready.SessionId, ready.SerieAPlayerId);
            var turnId = _turnMap.GetOrAdd(key, _ => Guid.NewGuid());
            _ = _timerManager.StartTimerAsync(turnId, ready.SessionId, 30, sessionId: ready.SessionId, serieAPlayerId: ready.SerieAPlayerId);
            _highestBids.TryRemove(key, out _);
        }
        else if (@event is NewHighestBidPlaced bid)
        {
            // Always track latest highest bid for the round
            var key = (bid.SessionId, bid.SerieAPlayerId);
            _highestBids[key] = (bid.TeamId, bid.Amount);
            // If a timer is known, extend it
            if (_turnMap.TryGetValue(key, out var turnId))
            {
                _ = _timerManager.UpdateTimerAsync(turnId, 30);
            }
            // Notify clients for new highest bid (session scoped)
            _ = NotifyNewHighestBidAsync(bid);
        }
        else if (@event is BiddingTimerExpired expired)
        {
            var key = (expired.SessionId, expired.SerieAPlayerId);
            if (_turnMap.TryRemove(key, out var turnId))
            {
                _ = _timerManager.StopTimerAsync(turnId);
            }
            if (_highestBids.TryRemove(key, out var win))
            {
                // Publish assignment event with winning bid
                _publisher.Publish(new PlayerAssignedViaBidding(expired.SessionId, win.TeamId, expired.SerieAPlayerId, RoleTypeFromPlayerIdPlaceholder(), win.Amount));
            }
        }
        else if (@event is BiddingReadyRequested readyRequested)
        {
            _ = NotifyReadyRequestedAsync(readyRequested);
        }
        else if (@event is Domain.Events.TurnAdvanced turn)
        {
            _ = NotifyTurnAdvancedAsync(turn);
        }
        else if (@event is Domain.Events.RoleAdvanced role)
        {
            _ = NotifyRoleAdvancedAsync(role);
        }
    }

    private async Task NotifyNewHighestBidAsync(NewHighestBidPlaced bid)
    {
        using var scope = _scopeFactory.CreateScope();
        var rt = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
        await rt.NewHighestBid(bid.SessionId, bid.SerieAPlayerId, bid.TeamId, bid.Amount);
    }

    private async Task NotifyReadyRequestedAsync(BiddingReadyRequested e)
    {
    using var scope = _scopeFactory.CreateScope();
        var rt = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
        await rt.BiddingReadyRequested(e.SessionId, e.NominatorTeamId, e.SerieAPlayerId, e.Role, e.EligibleOtherTeamIds);
    }

    private async Task NotifyTurnAdvancedAsync(Domain.Events.TurnAdvanced e)
    {
    using var scope = _scopeFactory.CreateScope();
        var rt = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
        await rt.TurnAdvanced(e.SessionId, e.NewOrderIndex, e.Role);
    }

    private async Task NotifyRoleAdvancedAsync(Domain.Events.RoleAdvanced e)
    {
    using var scope = _scopeFactory.CreateScope();
        var rt = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
        await rt.RoleAdvanced(e.SessionId, e.NewRole);
    }

    // Placeholder: in a real system we'd know the role from the session context or from the SerieA player lookup
    private static Domain.Enums.RoleType RoleTypeFromPlayerIdPlaceholder() => Domain.Enums.RoleType.P;
}
