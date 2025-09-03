using Domain.Entities;

namespace Domain.Events;

public record TurnFinalizedEvent(AuctionTurn Turn);
