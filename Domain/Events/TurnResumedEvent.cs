using Domain.Entities;

namespace Domain.Events;

public record TurnResumedEvent(AuctionTurn Turn);
