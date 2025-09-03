namespace Domain.Events;

public sealed record NewHighestBidPlaced(
    Guid SessionId,
    int SerieAPlayerId,
    Guid TeamId,
    int Amount
);
