using Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Infrastructure.Validators;

public class TeamValidator(ApplicationDbContext db) : ITeamValidator
{
    public async Task<bool> IsTeamNameUniqueAsync(Guid leagueId, string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return !await db.Teams
            .AnyAsync(t => t.LeagueId == leagueId && t.Name.ToLower() == normalized, ct);
    }
}
