namespace Application.Services;

public interface IRealtimeNotificationService
{
    Task TimerUpdate(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task TimerWarning(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task SendTimerUpdateAsync(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task SendTimerWarningAsync(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds);
    Task NewHighestBid(Guid sessionId, int serieAPlayerId, Guid teamId, int amount);
    Task BiddingReadyRequested(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId, Domain.Enums.RoleType role, IReadOnlyList<Guid> eligibleOtherTeamIds);
    Task BiddingReadyCompleted(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId, Domain.Enums.RoleType role, IReadOnlyList<Guid> eligibleOtherTeamIds);
    Task PlayerAssigned(Guid sessionId, int serieAPlayerId, Guid teamId, int amount);
    Task TurnAdvanced(Guid sessionId, int newOrderIndex, Domain.Enums.RoleType role);
    Task RoleAdvanced(Guid sessionId, Domain.Enums.RoleType newRole);
}
