namespace Domain.Events;

public sealed record TeamCreated(Guid TeamId, Guid LeagueId, string Name);
