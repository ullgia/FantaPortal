using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Services;

public record AuctionStateDto(
    Guid Id,
    AuctionStatus Status,
    PlayerType CurrentRole,
    Guid CurrentTurnTeamId,
    int? CurrentSerieAPlayerId,
    bool IsBiddingActive,
    bool IsReadyCheckActive,
    IReadOnlyList<BidDto> CurrentBids,
    IReadOnlyList<ReadyStateDto> ReadyStates
);

public record BidDto(
    Guid TeamId,
    string TeamName,
    int Amount,
    DateTime PlacedAt
);

public record ReadyStateDto(
    Guid TeamId,
    string TeamName,
    bool IsReady
);

public record TeamSummaryDto(
    Guid Id,
    string Name,
    int AvailableBudget,
    int SpentBudget,
    int PlayersCount,
    List<RoleSlotsSummaryDto> RoleSlots
);

public record RoleSlotsSummaryDto(
    PlayerType Role,
    int Used,
    int Max
);

public record LeagueStatsDto(
    Guid Id,
    string Name,
    int TotalBudget,
    int SpentBudget,
    int PlayersAssigned,
    int TotalPlayers,
    AuctionStatus? AuctionStatus,
    IReadOnlyList<TeamSummaryDto> Teams
);

public record TurnOrderDto(
    int Position,
    Guid TeamId,
    string TeamName,
    bool IsCurrentTurn
);

public record AuctionOverviewDto(
    Guid AuctionId,
    string LeagueName,
    AuctionStatus Status,
    PlayerType CurrentRole,
    int CurrentTurnPosition,
    int TotalTeams,
    Guid CurrentTurnTeamId,
    string CurrentTurnTeamName,
    IReadOnlyList<TurnOrderDto> TurnOrder,
    bool IsBiddingActive,
    bool IsReadyCheckActive
);

public record PlayerNominatedDto(
    int PlayerId,
    string PlayerName,
    PlayerType Role,
    string Team,
    decimal FVM,
    Guid NominatingTeamId,
    string NominatingTeamName
);

public interface IAuctionQueries
{
    Task<AuctionStateDto?> GetCurrentAuctionStateAsync(Guid leagueId, CancellationToken ct = default);
    Task<IReadOnlyList<TeamSummaryDto>> GetTeamsSummaryAsync(Guid leagueId, CancellationToken ct = default);
    Task<LeagueStatsDto?> GetLeagueStatsAsync(Guid leagueId, CancellationToken ct = default);
    Task<IReadOnlyList<BidDto>> GetCurrentBidsAsync(Guid leagueId, CancellationToken ct = default);
    Task<IReadOnlyList<ReadyStateDto>> GetReadyStatesAsync(Guid leagueId, CancellationToken ct = default);
    Task<IReadOnlyList<SerieAPlayer>> GetAvailablePlayersAsync(Guid leagueId, PlayerType role, CancellationToken ct = default);
    Task<AuctionOverviewDto?> GetAuctionOverviewAsync(Guid leagueId, CancellationToken ct = default);
    Task<IReadOnlyList<TurnOrderDto>> GetTurnOrderAsync(Guid leagueId, CancellationToken ct = default);
}
