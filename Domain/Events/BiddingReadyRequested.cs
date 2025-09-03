namespace Domain.Events;

using Domain.Enums;

public sealed record BiddingReadyRequested(Guid SessionId, Guid NominatorTeamId, int SerieAPlayerId, RoleType Role, IReadOnlyList<Guid> EligibleOtherTeamIds);
