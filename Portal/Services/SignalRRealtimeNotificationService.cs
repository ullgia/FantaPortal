using Application.Services;
using Microsoft.AspNetCore.SignalR;
using Portal.Hubs;

namespace Portal.Services;

public class SignalRRealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<AuctionHub> _hub;

    public SignalRRealtimeNotificationService(IHubContext<AuctionHub> hub)
    {
        _hub = hub;
    }

    public Task TimerUpdate(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds)
    {
        var tasks = new List<Task>
        {
            _hub.Clients.Group(AuctionHub.LeagueGroup(leagueId)).SendAsync("TimerUpdate", new { leagueId, auctionId, turnId, remainingSeconds }),
            _hub.Clients.Group(AuctionHub.AuctionGroup(auctionId)).SendAsync("TimerUpdate", new { leagueId, auctionId, turnId, remainingSeconds }),
            _hub.Clients.Group(AuctionHub.TurnGroup(turnId)).SendAsync("TimerUpdate", new { leagueId, auctionId, turnId, remainingSeconds })
        };
        return Task.WhenAll(tasks);
    }

    public Task TimerWarning(Guid leagueId, Guid auctionId, Guid turnId, int remainingSeconds)
    {
        var tasks = new List<Task>
        {
            _hub.Clients.Group(AuctionHub.LeagueGroup(leagueId)).SendAsync("TimerWarning", new { leagueId, auctionId, turnId, remainingSeconds }),
            _hub.Clients.Group(AuctionHub.AuctionGroup(auctionId)).SendAsync("TimerWarning", new { leagueId, auctionId, turnId, remainingSeconds }),
            _hub.Clients.Group(AuctionHub.TurnGroup(turnId)).SendAsync("TimerWarning", new { leagueId, auctionId, turnId, remainingSeconds })
        };
        return Task.WhenAll(tasks);
    }

    public Task NewHighestBid(Guid sessionId, int serieAPlayerId, Guid teamId, int amount)
        => _hub.Clients.Group(AuctionHub.SessionGroup(sessionId)).SendAsync("NewHighestBid", new { sessionId, serieAPlayerId, teamId, amount });

    public Task BiddingReadyRequested(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId, Domain.Enums.RoleType role, IReadOnlyList<Guid> eligibleOtherTeamIds)
        => _hub.Clients.Group(AuctionHub.SessionGroup(sessionId)).SendAsync("BiddingReadyRequested", new { sessionId, nominatorTeamId, serieAPlayerId, role, eligibleOtherTeamIds });

    public Task BiddingReadyCompleted(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId, Domain.Enums.RoleType role, IReadOnlyList<Guid> eligibleOtherTeamIds)
        => _hub.Clients.Group(AuctionHub.SessionGroup(sessionId)).SendAsync("BiddingReadyCompleted", new { sessionId, nominatorTeamId, serieAPlayerId, role, eligibleOtherTeamIds });

    public Task PlayerAssigned(Guid sessionId, int serieAPlayerId, Guid teamId, int amount)
        => _hub.Clients.Group(AuctionHub.SessionGroup(sessionId)).SendAsync("PlayerAssigned", new { sessionId, serieAPlayerId, teamId, amount });

    public Task TurnAdvanced(Guid sessionId, int newOrderIndex, Domain.Enums.RoleType role)
        => _hub.Clients.Group(AuctionHub.SessionGroup(sessionId)).SendAsync("TurnAdvanced", new { sessionId, newOrderIndex, role });

    public Task RoleAdvanced(Guid sessionId, Domain.Enums.RoleType newRole)
        => _hub.Clients.Group(AuctionHub.SessionGroup(sessionId)).SendAsync("RoleAdvanced", new { sessionId, newRole });
}
