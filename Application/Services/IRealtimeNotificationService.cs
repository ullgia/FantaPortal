using Domain.ValueObjects;

namespace Application.Services;

public record PlayerNominationEvent(
    int PlayerId,
    string PlayerName,
    Domain.Enums.PlayerType Role,
    string Team,
    decimal FVM,
    Guid NominatingTeamId,
    string NominatingTeamName
);

public interface IRealtimeNotificationService
{
    Task TimerUpdate(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task TimerWarning(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task SendTimerUpdateAsync(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task SendTimerWarningAsync(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task NewHighestBid(Guid sessionId, int serieAPlayerId, Guid teamId, int amount);
    Task BiddingReadyRequested(Guid sessionId, PlayerNominationEvent playerNomination, IReadOnlyList<Guid> eligibleOtherTeamIds);
    Task BiddingReadyCompleted(Guid sessionId, PlayerNominationEvent playerNomination, IReadOnlyList<Guid> eligibleOtherTeamIds);
    Task BiddingPhaseStarted(Guid sessionId, BiddingInfo biddingInfo, int timerDurationSeconds);
    Task BiddingTimerUpdate(Guid sessionId, int remainingSeconds);
    Task PlayerAssigned(Guid sessionId, int serieAPlayerId, Guid teamId, int amount);
    Task TurnAdvanced(Guid sessionId, int newOrderIndex, Domain.Enums.PlayerType role);
    Task RoleAdvanced(Guid sessionId, Domain.Enums.PlayerType newRole);
}
