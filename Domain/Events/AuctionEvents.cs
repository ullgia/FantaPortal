namespace Domain.Events;

using Domain.ValueObjects;

// Eventi per avvio/gestione timer
public sealed record AuctionStarted(
    Guid LeagueId, 
    Guid SessionId, 
    TurnInfo CurrentTurn)
{
    public int TimerSeconds => 60; // Default turn timer
}

public sealed record BiddingPhaseStarted(
    Guid LeagueId, 
    BiddingInfo BiddingInfo)
{
    public int TimerSeconds => 30; // Default bidding timer  
}

public sealed record BidPlaced(
    Guid LeagueId,
    Guid TeamId, 
    int Amount,
    int RemainingTime)
{
    // Per reset timer UI se necessario
}

// Eventi per stop timer e avanzamento UI
public sealed record PlayerAssigned(
    Guid LeagueId,
    Guid TeamId,
    int PlayerId,
    int Price,
    TurnInfo NextTurn)
{
    public bool StopTimer => true;
}

public sealed record BiddingRoundFinalized(
    Guid LeagueId,
    Guid WinnerTeamId,
    int PlayerId, 
    int Amount,
    TurnInfo NextTurn)
{
    public bool StopTimer => true;
}

// Eventi per pausa/ripresa (timer management)
public sealed record AuctionPaused(Guid LeagueId, Guid SessionId)
{
    public bool PauseTimer => true;
}

public sealed record AuctionResumed(Guid LeagueId, Guid SessionId, TurnInfo CurrentTurn)
{
    public bool ResumeTimer => true;
}

// Eventi per UI updates senza logica business
public sealed record TurnForced(Guid LeagueId, TurnInfo NextTurn);
public sealed record AuctionCompleted(Guid LeagueId, Guid SessionId, LeagueStatistics FinalStats);
public sealed record AssignmentUndone(Guid LeagueId, Guid TeamId, int PlayerId);
public sealed record AuctionOrderChanged(Guid LeagueId, IReadOnlyList<Guid> NewOrder);
