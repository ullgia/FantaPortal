namespace Domain.Events;

using Domain.Enums;

public sealed record PlayerAutoAssigned(Guid SessionId, Guid TeamId, int SerieAPlayerId, PlayerType Role, int Price);
