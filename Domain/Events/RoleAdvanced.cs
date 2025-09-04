namespace Domain.Events;

using Domain.Enums;

public sealed record RoleAdvanced(Guid SessionId, PlayerType NewRole);
