using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Infrastructure.Services;

public sealed class AuctionCommands(ApplicationDbContext db) : IAuctionCommands
{
    private readonly ApplicationDbContext _db = db;

    public async Task NominateAsync(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId, CancellationToken ct = default)
    {
        var session = await _db.AuctionSessions.FindAsync([sessionId], ct) ?? throw new InvalidOperationException("Session not found");
        var serieA = await _db.SerieAPlayers.FirstOrDefaultAsync(p => p.Id == serieAPlayerId, ct) ?? throw new InvalidOperationException("Player not found");
        var participants = await _db.AuctionParticipants.Where(p => p.SessionId == sessionId).ToListAsync(ct);
        var teams = await _db.Teams.Where(t => t.LeagueId == session.LeagueId).ToListAsync(ct);
        var order = participants.OrderBy(p => p.OrderIndex).Select(p => p.TeamId).ToList();
        var teamsMap = teams.ToDictionary(t => t.Id);
        session.Nominate(order, teamsMap, nominatorTeamId, serieA);
        _db.Update(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkReadyAsync(Guid sessionId, Guid teamId, CancellationToken ct = default)
    {
        var session = await _db.AuctionSessions.FindAsync([sessionId], ct) ?? throw new InvalidOperationException("Session not found");
        session.MarkReady(teamId);
        _db.Update(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PlaceBidAsync(Guid sessionId, Guid teamId, int amount, CancellationToken ct = default)
    {
        var session = await _db.AuctionSessions.FindAsync([sessionId], ct) ?? throw new InvalidOperationException("Session not found");
        session.PlaceBid(teamId, amount);
        _db.Update(session);
        await _db.SaveChangesAsync(ct);
    }
}
