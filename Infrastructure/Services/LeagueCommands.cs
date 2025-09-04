using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Infrastructure.Peristance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class LeagueCommands(ApplicationDbContext db) : ILeagueCommands
{
    private readonly ApplicationDbContext _db = db;

    public async Task<Guid> CreateLeagueAsync(string name, CancellationToken ct = default)
    {
        var league = League.Create(name);
        await _db.Leagues.AddAsync(league, ct);
        await _db.SaveChangesAsync(ct);
        return league.Id;
    }

    public async Task<Guid> AddTeamAsync(Guid leagueId, string teamName, int initialBudget, CancellationToken ct = default)
    {
        var league = await _db.Leagues.Include(l => l.Teams).FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                     ?? throw new InvalidOperationException("League not found");

        var team = league.AddTeam(teamName, initialBudget);
        await _db.SaveChangesAsync(ct);
        return team.Id;
    }
}
