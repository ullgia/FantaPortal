namespace Application.Events.Handlers;

using Application.Events;
using Application.Services;
using Domain.Events;
using Domain.Enums;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler per bid che aggiorna il timer con incremento calcolato e la UI
/// </summary>
public sealed class BidPlacedHandler : IDomainEventHandler<BidPlaced>
{
    private readonly IAuctionTimerManager _timerManager;
    private readonly IRealtimeNotificationService _notificationService;
    private readonly ILeagueQueries _leagueQueries;
    private readonly ILogger<BidPlacedHandler> _logger;

    public BidPlacedHandler(
        IAuctionTimerManager timerManager,
        IRealtimeNotificationService notificationService,
        ILeagueQueries leagueQueries,
        ILogger<BidPlacedHandler> logger)
    {
        _timerManager = timerManager;
        _notificationService = notificationService;
        _leagueQueries = leagueQueries;
        _logger = logger;
    }

    public async Task Handle(BidPlaced @event)
    {
        try
        {
            _logger.LogInformation("Handling BidPlaced for session {SessionId}, bid {Amount} by team {TeamId}",
                @event.SessionId, @event.Amount, @event.TeamId);


            await _timerManager.UpdateTimerAsync(@event.SessionId, @event.RemainingTime);

            // 3. Notifica tutti i client del nuovo bid
            await _notificationService.NewHighestBid(@event.SessionId, @event.TeamId, @event.Amount);

            _logger.LogInformation("Successfully handled BidPlaced: extended timer by {Increment}s and notified clients", @event.RemainingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BidPlaced for session {SessionId}", @event.SessionId);
        }
    }

  
}
