namespace Domain.Events;

using Domain.Enums;

public sealed record PlayerAssignedViaBidding(
    Guid SessionId,
    Guid TeamId,
    int SerieAPlayerId,
    RoleType Role,
    int Amount
);
