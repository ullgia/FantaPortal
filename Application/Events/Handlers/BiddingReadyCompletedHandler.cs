namespace Application.Events.Handlers;

using Application.Events;
using Application.Services;
using Domain.Events;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler per completamento ready-check che avvia il bidding e il timer
/// </summary>
public sealed class BiddingReadyCompletedHandler : IDomainEventHandler<BiddingReadyCompleted>
{
    private readonly IAuctionCommands _auctionCommands;
    private readonly IAuctionTimerManager _timerManager;
    private readonly IRealtimeNotificationService _notificationService;
    private readonly ILogger<BiddingReadyCompletedHandler> _logger;

    public BiddingReadyCompletedHandler(
        IAuctionCommands auctionCommands,
        IAuctionTimerManager timerManager,
        IRealtimeNotificationService notificationService,
        ILogger<BiddingReadyCompletedHandler> logger)
    {
        _auctionCommands = auctionCommands;
        _timerManager = timerManager;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(BiddingReadyCompleted @event)
    {
        try
        {
            _logger.LogInformation("Handling BiddingReadyCompleted for session {SessionId}, player {PlayerId}, timer {TimerSeconds}s", 
                @event.SessionId, @event.SerieAPlayerId, @event.TimerSeconds);

            // 1. Avvia il bidding nella sessione
            var biddingInfo = await _auctionCommands.StartBiddingAfterReadyAsync(@event.SessionId);
            
            if (biddingInfo == null)
            {
                _logger.LogWarning("Failed to start bidding for session {SessionId}", @event.SessionId);
                return;
            }

            // 2. Avvia il timer per il bidding usando i secondi dall'evento
            await _timerManager.StartBiddingTimerAsync(@event.SessionId, @event.TimerSeconds);

            // 3. Notifica tutti i client che il bidding è iniziato
            await _notificationService.BiddingPhaseStarted(@event.SessionId, biddingInfo, @event.TimerSeconds);

            _logger.LogInformation("Successfully started bidding phase for session {SessionId} with {Duration}s timer", 
                @event.SessionId, @event.TimerSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BiddingReadyCompleted for session {SessionId}", @event.SessionId);
            throw;
        }
    }
}
