using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Application.Events;

namespace Portal.Hubs;

public class AuctionHub : Hub
{
    public static string LeagueGroup(Guid leagueId) => $"league:{leagueId}";
    public static string AuctionGroup(Guid auctionId) => $"auction:{auctionId}";
    public static string TurnGroup(Guid turnId) => $"turn:{turnId}";
    public static string SessionGroup(Guid sessionId) => $"session:{sessionId}";

    public Task JoinLeague(Guid leagueId)
        => Groups.AddToGroupAsync(Context.ConnectionId, LeagueGroup(leagueId));

    public Task LeaveLeague(Guid leagueId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, LeagueGroup(leagueId));

    public Task JoinAuction(Guid auctionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));

    public Task LeaveAuction(Guid auctionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, AuctionGroup(auctionId));

    public Task JoinTurn(Guid turnId)
        => Groups.AddToGroupAsync(Context.ConnectionId, TurnGroup(turnId));

    public Task LeaveTurn(Guid turnId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, TurnGroup(turnId));

    public Task JoinSession(Guid sessionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));

    public Task LeaveSession(Guid sessionId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(sessionId));

    // Commands (intra-progetto): invocati dai client Blazor via SignalR
    public async Task Nominate(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId, [FromServices] Application.Services.IAuctionCommands commands)
        => await commands.NominatePlayerAsync(sessionId, nominatorTeamId, serieAPlayerId, Context.ConnectionAborted);

    public async Task PlaceBid(Guid sessionId, Guid teamId, int amount, [FromServices] Application.Services.IAuctionCommands commands)
        => await commands.PlaceBidAsync(sessionId, teamId, amount, Context.ConnectionAborted);

    // Metodi per aggiornamenti ottimizzati (chiamati dal backend)
    public async Task SendTurnOrderUpdate(Guid leagueId, IReadOnlyList<Application.Services.TurnOrderDto> turnOrder)
        => await Clients.Group(LeagueGroup(leagueId)).SendAsync(SignalREventNames.TurnOrderUpdate, turnOrder);

    public async Task SendReadyStatesUpdate(Guid leagueId, IReadOnlyList<Application.Services.ReadyStateDto> readyStates)
        => await Clients.Group(LeagueGroup(leagueId)).SendAsync(SignalREventNames.ReadyStatesUpdate, readyStates);

    public async Task SendPlayerNominated(Guid leagueId, Application.Services.PlayerNominatedDto playerNominated)
        => await Clients.Group(LeagueGroup(leagueId)).SendAsync(SignalREventNames.PlayerNominated, playerNominated);

    public async Task SendBidUpdate(Guid leagueId, Application.Services.BidDto bid)
        => await Clients.Group(LeagueGroup(leagueId)).SendAsync(SignalREventNames.BidUpdate, bid);

    public async Task SendFullStateUpdate(Guid leagueId, Application.Services.AuctionOverviewDto overview)
        => await Clients.Group(LeagueGroup(leagueId)).SendAsync(SignalREventNames.FullStateUpdate, overview);

    public async Task SendPhaseChanged(Guid leagueId, string newPhase)
        => await Clients.Group(LeagueGroup(leagueId)).SendAsync(SignalREventNames.PhaseChanged, newPhase);
}
