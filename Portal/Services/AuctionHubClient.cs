using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Portal.Services;

public class AuctionHubClient : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _connection;

    public event Action<Guid, Guid, Guid, int>? TimerUpdate;
    public event Action<Guid, Guid, Guid, int>? TimerWarning;
    public event Action<Guid, int, Guid, int>? NewHighestBid; // sessionId, playerId, teamId, amount
    public event Action<Guid, Guid, int, string, Guid[]>? ReadyRequested; // sessionId, nominatorTeamId, playerId, role, eligibleTeamIds
    public event Action<Guid, Guid, int, string, Guid[]>? ReadyCompleted;
    public event Action<Guid, int, Guid, int>? PlayerAssigned; // sessionId, playerId, teamId, amount
    public event Action<Guid, int, string>? TurnAdvanced; // sessionId, newOrderIndex, role
    public event Action<Guid, string>? RoleAdvanced; // sessionId, newRole

    public AuctionHubClient(NavigationManager nav)
    {
        _nav = nav;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_connection != null) return;
        _connection = new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/hubs/auction"))
            .WithAutomaticReconnect()
            .Build();

        _connection.On<object>("TimerUpdate", payload =>
        {
            var d = (JsonElement)payload;
            var leagueId = d.GetProperty("leagueId").GetGuid();
            var auctionId = d.GetProperty("auctionId").GetGuid();
            var turnId = d.GetProperty("turnId").GetGuid();
            var remainingSeconds = d.GetProperty("remainingSeconds").GetInt32();
            TimerUpdate?.Invoke(leagueId, auctionId, turnId, remainingSeconds);
        });
        _connection.On<object>("TimerWarning", payload =>
        {
            var d = (JsonElement)payload;
            var leagueId = d.GetProperty("leagueId").GetGuid();
            var auctionId = d.GetProperty("auctionId").GetGuid();
            var turnId = d.GetProperty("turnId").GetGuid();
            var remainingSeconds = d.GetProperty("remainingSeconds").GetInt32();
            TimerWarning?.Invoke(leagueId, auctionId, turnId, remainingSeconds);
        });

        _connection.On<object>("NewHighestBid", payload =>
        {
            var d = (JsonElement)payload;
            NewHighestBid?.Invoke(
                d.GetProperty("sessionId").GetGuid(),
                d.GetProperty("serieAPlayerId").GetInt32(),
                d.GetProperty("teamId").GetGuid(),
                d.GetProperty("amount").GetInt32());
        });
        _connection.On<object>("BiddingReadyRequested", payload =>
        {
            var d = (JsonElement)payload;
            ReadyRequested?.Invoke(
                d.GetProperty("sessionId").GetGuid(),
                d.GetProperty("nominatorTeamId").GetGuid(),
                d.GetProperty("serieAPlayerId").GetInt32(),
                d.GetProperty("role").GetString()!,
                d.GetProperty("eligibleOtherTeamIds").EnumerateArray().Select(x => x.GetGuid()).ToArray());
        });
        _connection.On<object>("BiddingReadyCompleted", payload =>
        {
            var d = (JsonElement)payload;
            ReadyCompleted?.Invoke(
                d.GetProperty("sessionId").GetGuid(),
                d.GetProperty("nominatorTeamId").GetGuid(),
                d.GetProperty("serieAPlayerId").GetInt32(),
                d.GetProperty("role").GetString()!,
                d.GetProperty("eligibleOtherTeamIds").EnumerateArray().Select(x => x.GetGuid()).ToArray());
        });
        _connection.On<object>("PlayerAssigned", payload =>
        {
            var d = (JsonElement)payload;
            PlayerAssigned?.Invoke(
                d.GetProperty("sessionId").GetGuid(),
                d.GetProperty("serieAPlayerId").GetInt32(),
                d.GetProperty("teamId").GetGuid(),
                d.GetProperty("amount").GetInt32());
        });
        _connection.On<object>("TurnAdvanced", payload =>
        {
            var d = (JsonElement)payload;
            TurnAdvanced?.Invoke(
                d.GetProperty("sessionId").GetGuid(),
                d.GetProperty("newOrderIndex").GetInt32(),
                d.GetProperty("role").GetString()!);
        });
        _connection.On<object>("RoleAdvanced", payload =>
        {
            var d = (JsonElement)payload;
            RoleAdvanced?.Invoke(
                d.GetProperty("sessionId").GetGuid(),
                d.GetProperty("newRole").GetString()!);
        });

        await _connection.StartAsync(ct);
    }

    public async Task JoinLeagueAsync(Guid leagueId) => await _connection!.InvokeAsync("JoinLeague", leagueId);
    public async Task LeaveLeagueAsync(Guid leagueId) => await _connection!.InvokeAsync("LeaveLeague", leagueId);
    public async Task JoinAuctionAsync(Guid auctionId) => await _connection!.InvokeAsync("JoinAuction", auctionId);
    public async Task LeaveAuctionAsync(Guid auctionId) => await _connection!.InvokeAsync("LeaveAuction", auctionId);
    public async Task JoinTurnAsync(Guid turnId) => await _connection!.InvokeAsync("JoinTurn", turnId);
    public async Task LeaveTurnAsync(Guid turnId) => await _connection!.InvokeAsync("LeaveTurn", turnId);
    public async Task JoinSessionAsync(Guid sessionId) => await _connection!.InvokeAsync("JoinSession", sessionId);
    public async Task LeaveSessionAsync(Guid sessionId) => await _connection!.InvokeAsync("LeaveSession", sessionId);

    // Commands wrappers
    public async Task NominateAsync(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId)
        => await _connection!.InvokeAsync("Nominate", sessionId, nominatorTeamId, serieAPlayerId);

    public async Task MarkReadyAsync(Guid sessionId, Guid teamId)
        => await _connection!.InvokeAsync("MarkReady", sessionId, teamId);

    public async Task PlaceBidAsync(Guid sessionId, Guid teamId, int amount)
        => await _connection!.InvokeAsync("PlaceBid", sessionId, teamId, amount);

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
