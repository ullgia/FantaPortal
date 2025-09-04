namespace Domain.Events;

using Domain.Enums;

public sealed record PlayerAssignedViaBidding(
    Guid SessionId,
    Guid TeamId,
    int SerieAPlayerId,
    PlayerType Role,
    int Amount
);
