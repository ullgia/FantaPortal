using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

public interface ILeagueCommands
{
    Task<Guid> CreateLeagueAsync(string name, CancellationToken ct = default);
    Task<Guid> AddTeamAsync(Guid leagueId, string teamName, int initialBudget, CancellationToken ct = default);
}
