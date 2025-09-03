namespace Application.Events;

public sealed record BiddingTimerExpired(
    Guid TurnId,
    Guid SessionId,
    int SerieAPlayerId
);
