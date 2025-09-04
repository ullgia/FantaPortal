namespace Domain.Events;

using Domain.Enums;

public sealed record TurnAdvanced(Guid SessionId, int NewOrderIndex, PlayerType Role);
