using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Infrastructure.Peristance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class LeagueQueries(ApplicationDbContext db) : ILeagueQueries
{
    private readonly ApplicationDbContext _db = db;

    public async Task<IReadOnlyList<LeagueListItemDto>> GetLeaguesAsync(CancellationToken ct = default)
    {
        var leagues = await _db.Leagues
            .Include(l => l.ActiveAuction)
            .Include(l => l.Teams)
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

        return leagues.Select(l => new LeagueListItemDto(
            l.Id,
            l.Name,
            l.Teams.Count,
            l.ActiveAuction != null,
            l.ActiveAuction?.Status.ToString()
        )).ToList();
    }

    public async Task<int> GetBiddingBaseSecondsAsync(Guid leagueId, CancellationToken ct = default)
    {
        var league = await _db.Leagues
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct);

        return league?.BiddingBaseSeconds ?? 30; // fallback
    }

    public async Task<int> GetBiddingBaseSecondsBySessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var league = await _db.Leagues
            .Include(l => l.ActiveAuction)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ActiveAuction != null && l.ActiveAuction.Id == sessionId, ct);

        return league?.BiddingBaseSeconds ?? 30; // fallback
    }

    // Le funzioni user-team vengono ora gestite direttamente interrogando Teams.OwnerUserId
}
