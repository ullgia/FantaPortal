using Domain.Contracts;
using Infrastructure.Peristance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Validators;

public class TeamValidator(ApplicationDbContext db) : ITeamValidator
{
    public async Task<bool> IsTeamNameUniqueAsync(Guid leagueId, string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        
        // Check if any league has a team with this name
        var league = await db.Leagues
            .Include(l => l.Teams)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct);
            
        if (league == null) return true;
        
        return !league.Teams.Any(t => t.Name.ToLowerInvariant() == normalized);
    }
}
