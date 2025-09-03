namespace Domain.ValueObjects;

using Domain.Entities;
using Domain.Enums;

// Informazioni stato corrente
public record TurnInfo(
    Guid CurrentTeamId,
    RoleType CurrentRole, 
    int OrderIndex,
    AuctionStatus Status)
{
    public static TurnInfo Empty => new(Guid.Empty, RoleType.P, 0, AuctionStatus.Preparation);
}

public record BiddingInfo(
    Guid NominatorId,
    int PlayerId,
    Guid HighestBidder,
    int HighestBid,
    IReadOnlyList<Guid> EligibleTeams,
    int RemainingTime);

// Risultati operazioni
public record NominationResult(
    bool IsAutoAssign,
    bool IsReadyCheck,
    RoleType Role,
    int Price,
    BiddingInfo? BiddingInfo,
    BiddingReadyState? ReadyState)
{
    public static NominationResult AutoAssign(RoleType role, int price) =>
        new(true, false, role, price, null, null);
        
    public static NominationResult StartBidding(RoleType role, BiddingInfo biddingInfo) =>
        new(false, false, role, 0, biddingInfo, null);
        
    public static NominationResult StartReadyCheck(RoleType role, IReadOnlyList<Guid> eligibleTeams, BiddingReadyState readyState) =>
        new(false, true, role, 0, null, readyState);
}

public record BidResult(int Amount, int TimeRemaining);
public record WinningBid(Guid TeamId, int Amount);

// Statistiche e summary  
public record PlayerCounts(int P, int D, int C, int A);

public record TeamSummary(
    Guid Id,
    string Name,
    int Budget,
    PlayerCounts Players,
    int TotalOwnerships);

public record LeagueStatistics(
    int TotalTeams,
    int TotalPlayers,
    int TotalBudgetRemaining,
    int TotalSpent);

public record AuctionState(
    AuctionStatus Status,
    TurnInfo? CurrentTurn,
    BiddingInfo? CurrentBidding,
    IReadOnlyList<TeamSummary> Teams)
{
    public static AuctionState NoAuction() => 
        new(AuctionStatus.Preparation, null, null, Array.Empty<TeamSummary>());
}
