namespace Domain.Events;

using Domain.Enums;

public sealed record BiddingReadyCompleted(
    Guid SessionId,
    Guid NominatorTeamId,
    int SerieAPlayerId,
    PlayerType Role,
    IReadOnlyList<Guid> EligibleOtherTeamIds,
    int TimerSeconds
);
