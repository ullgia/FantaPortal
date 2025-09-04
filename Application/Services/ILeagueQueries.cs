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
    Task<int> GetBiddingBaseSecondsAsync(Guid leagueId, CancellationToken ct = default);
    Task<int> GetBiddingBaseSecondsBySessionAsync(Guid sessionId, CancellationToken ct = default);
}
