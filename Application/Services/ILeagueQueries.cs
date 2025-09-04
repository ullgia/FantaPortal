using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

public record LeagueListItemDto(
    Guid Id,
    string Name,
    int TeamsCount,
    bool HasActiveAuction,
    string? ActiveAuctionStatus
);

public interface ILeagueQueries
{
    Task<IReadOnlyList<LeagueListItemDto>> GetLeaguesAsync(CancellationToken ct = default);
}
