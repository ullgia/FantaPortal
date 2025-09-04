namespace Domain.Events;

using Domain.Enums;
using Domain.ValueObjects;

// Eventi per avvio/gestione timer
public sealed record AuctionStarted(
    Guid LeagueId, 
    Guid SessionId, 
    TurnInfo CurrentTurn)
{
    public int TimerSeconds => 60; // Default turn timer
}

public sealed record BidPlaced(
    Guid LeagueId,
    Guid SessionId,
    Guid TeamId, 
    int Amount,
    int RemainingTime)
{
    // Evento per aggiornare timer e UI
}

// Eventi per stop timer e avanzamento UI
public sealed record PlayerAssigned(
    Guid LeagueId,
    Guid SessionId,
    Guid TeamId,
    int PlayerId,
    int Price,
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
