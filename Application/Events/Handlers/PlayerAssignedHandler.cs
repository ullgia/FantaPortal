namespace Application.Events.Handlers;

using Application.Events;
using Application.Services;
using Domain.Events;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler per assegnazione giocatore che aggiorna la UI tramite SignalR
/// </summary>
public sealed class PlayerAssignedHandler : IDomainEventHandler<PlayerAssigned>
{
    private readonly IRealtimeNotificationService _notificationService;
    private readonly ILogger<PlayerAssignedHandler> _logger;

    public PlayerAssignedHandler(
        IRealtimeNotificationService notificationService,
        ILogger<PlayerAssignedHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(PlayerAssigned @event)
    {
        try
        {
            _logger.LogInformation("Handling PlayerAssigned for session {SessionId}, player {PlayerId} assigned to team {TeamId} for {Price}", 
                @event.SessionId, @event.PlayerId, @event.TeamId, @event.Price);

            // Notifica tutti i client dell'assegnazione del giocatore
            await _notificationService.PlayerAssigned(@event.SessionId, @event.PlayerId, @event.TeamId, @event.Price);

            _logger.LogInformation("Successfully notified PlayerAssigned for session {SessionId}", @event.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PlayerAssigned for session {SessionId}", @event.SessionId);
        }
    }
}
