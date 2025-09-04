using Application.Services;
using Application.Events;
using Microsoft.AspNetCore.SignalR;
using Portal.Hubs;

namespace Portal.Services;

public class SignalRNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public SignalRNotificationService(IHubContext<AuctionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTurnOrderChanged(Guid leagueId, IReadOnlyList<TurnOrderDto> turnOrder)
    {
        await _hubContext.Clients.Group(AuctionHub.LeagueGroup(leagueId))
            .SendAsync(SignalREventNames.TurnOrderUpdate, turnOrder);
    }

    public async Task NotifyReadyStatesChanged(Guid leagueId, IReadOnlyList<ReadyStateDto> readyStates)
    {
        await _hubContext.Clients.Group(AuctionHub.LeagueGroup(leagueId))
            .SendAsync(SignalREventNames.ReadyStatesUpdate, readyStates);
    }

    public async Task NotifyFullStateUpdate(Guid leagueId, AuctionOverviewDto overview)
    {
        await _hubContext.Clients.Group(AuctionHub.LeagueGroup(leagueId))
            .SendAsync(SignalREventNames.FullStateUpdate, overview);
    }

    public async Task NotifyBidPlaced(Guid leagueId, BidDto bid)
    {
        await _hubContext.Clients.Group(AuctionHub.LeagueGroup(leagueId))
            .SendAsync(SignalREventNames.BidUpdate, bid);
    }

    public async Task NotifyPlayerNominated(Guid leagueId, PlayerNominatedDto playerNominated)
    {
        await _hubContext.Clients.Group(AuctionHub.LeagueGroup(leagueId))
            .SendAsync(SignalREventNames.PlayerNominated, playerNominated);
    }

    public async Task NotifyPhaseChanged(Guid leagueId, string newPhase)
    {
        await _hubContext.Clients.Group(AuctionHub.LeagueGroup(leagueId))
            .SendAsync(SignalREventNames.PhaseChanged, newPhase);
    }
}
