namespace Domain.Events;

public sealed record AuctionSessionStarted(Guid SessionId, Guid LeagueId);
