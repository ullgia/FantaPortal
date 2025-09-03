using Domain.Entities;

namespace Domain.Events;

public record TurnCreatedEvent(AuctionTurn Turn);
