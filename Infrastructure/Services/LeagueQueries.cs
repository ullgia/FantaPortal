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

    // Le funzioni user-team vengono ora gestite direttamente interrogando Teams.OwnerUserId
}
