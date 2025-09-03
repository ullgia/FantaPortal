using Domain.Entities;
using Domain.Enums;

namespace Domain.Events;

public record AuctionStatusChangedEvent(
    AuctionSession AuctionEvent,
    AuctionStatus PreviousStatus,
    AuctionStatus NewStatus);
