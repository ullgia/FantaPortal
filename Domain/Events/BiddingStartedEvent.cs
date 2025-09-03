using Domain.Entities;

namespace Domain.Events;

public record BiddingStartedEvent(AuctionTurn Turn);
