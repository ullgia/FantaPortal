using Domain.Entities;

namespace Domain.Events;

public record TurnPausedEvent(AuctionTurn Turn);
