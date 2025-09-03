using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

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
        => await commands.NominateAsync(sessionId, nominatorTeamId, serieAPlayerId, Context.ConnectionAborted);

    public async Task PlaceBid(Guid sessionId, Guid teamId, int amount, [FromServices] Application.Services.IAuctionCommands commands)
        => await commands.PlaceBidAsync(sessionId, teamId, amount, Context.ConnectionAborted);
}
