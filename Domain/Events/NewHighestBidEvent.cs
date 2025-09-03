namespace Domain.Events;

public record NewHighestBidEvent(Guid TurnId, Guid BidId, decimal Amount);
