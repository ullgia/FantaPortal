using Domain.ValueObjects;

namespace Application.Services;

public class NoOpRealtimeNotificationService : IRealtimeNotificationService
{
    public Task TimerUpdate(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds) => Task.CompletedTask;
    public Task TimerWarning(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds) => Task.CompletedTask;
    public Task SendTimerUpdateAsync(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds) => Task.CompletedTask;
    public Task SendTimerWarningAsync(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds) => Task.CompletedTask;
    public Task NewHighestBid(Guid sessionId, int serieAPlayerId, Guid teamId, int amount) => Task.CompletedTask;
    public Task BiddingReadyRequested(Guid sessionId, PlayerNominationEvent playerNomination, IReadOnlyList<Guid> eligibleOtherTeamIds) => Task.CompletedTask;
    public Task BiddingReadyCompleted(Guid sessionId, PlayerNominationEvent playerNomination, IReadOnlyList<Guid> eligibleOtherTeamIds) => Task.CompletedTask;
    public Task BiddingPhaseStarted(Guid sessionId, BiddingInfo biddingInfo, int timerDurationSeconds) => Task.CompletedTask;
    public Task BiddingTimerUpdate(Guid sessionId, int remainingSeconds) => Task.CompletedTask;
    public Task PlayerAssigned(Guid sessionId, int serieAPlayerId, Guid teamId, int amount) => Task.CompletedTask;
    public Task TurnAdvanced(Guid sessionId, int newOrderIndex, Domain.Enums.PlayerType role) => Task.CompletedTask;
    public Task RoleAdvanced(Guid sessionId, Domain.Enums.PlayerType newRole) => Task.CompletedTask;
}
