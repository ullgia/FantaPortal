namespace Domain.ValueObjects;

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
    RoleType Role,
    int Price,
    BiddingInfo? BiddingInfo)
{
    public static NominationResult AutoAssign(RoleType role, int price) =>
        new(true, role, price, null);
        
    public static NominationResult StartBidding(RoleType role, BiddingInfo biddingInfo) =>
        new(false, role, 0, biddingInfo);
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
