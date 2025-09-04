using Application.Services;

namespace Portal.Services;

public interface IRealtimeNotificationService
{
    Task NotifyTurnOrderChanged(Guid leagueId, IReadOnlyList<TurnOrderDto> turnOrder);
    Task NotifyReadyStatesChanged(Guid leagueId, IReadOnlyList<ReadyStateDto> readyStates);
    Task NotifyFullStateUpdate(Guid leagueId, AuctionOverviewDto overview);
    Task NotifyBidPlaced(Guid leagueId, BidDto bid);
    Task NotifyPlayerNominated(Guid leagueId, PlayerNominatedDto playerNominated);
    Task NotifyPhaseChanged(Guid leagueId, string newPhase);
}
