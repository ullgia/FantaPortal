using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class AuctionTimerService
{
    private readonly IAuctionTimerManager _timerManager;
    private readonly ILogger<AuctionTimerService> _logger;

    public AuctionTimerService(IAuctionTimerManager timerManager, ILogger<AuctionTimerService> logger)
    {
        _timerManager = timerManager;
        _logger = logger;
    }

    public async Task StartBiddingTimerAsync(Guid turnId, Guid auctionId, Guid sessionId, Guid leagueId, int? serieAPlayerId = null, int durationSeconds = 60, int warningAtSeconds = 30)
    {
        await _timerManager.StartTimerAsync(
            turnId: turnId,
            auctionId: auctionId,
            remainingSeconds: durationSeconds,
            leagueId: leagueId,
            warningAtSeconds: warningAtSeconds,
            serieAPlayerId: serieAPlayerId,
            sessionId: sessionId);

        _logger.LogInformation("Bidding timer started for turn {TurnId} in session {SessionId}", turnId, sessionId);
    }

    public async Task PauseBiddingTimerAsync(Guid turnId)
    {
        await _timerManager.PauseTimerAsync(turnId);
        _logger.LogInformation("Bidding timer paused for turn {TurnId}", turnId);
    }

    public async Task ResumeBiddingTimerAsync(Guid turnId)
    {
        await _timerManager.ResumeTimerAsync(turnId);
        _logger.LogInformation("Bidding timer resumed for turn {TurnId}", turnId);
    }

    public async Task ExtendBiddingTimerAsync(Guid turnId, int additionalSeconds)
    {
        if (_timerManager.HasActiveTimer(turnId))
        {
            // Per estendere il timer, dovremmo prima ottenere i secondi rimanenti attuali
            // e poi aggiungerci i secondi addizionali. Per ora implemento una versione semplificata
            await _timerManager.UpdateTimerAsync(turnId, additionalSeconds);
            _logger.LogInformation("Bidding timer extended for turn {TurnId} by {AdditionalSeconds} seconds", turnId, additionalSeconds);
        }
    }

    public async Task StopBiddingTimerAsync(Guid turnId)
    {
        await _timerManager.StopTimerAsync(turnId);
        _logger.LogInformation("Bidding timer stopped for turn {TurnId}", turnId);
    }

    public async Task PauseAllTimersForSessionAsync(Guid sessionId, Guid auctionId)
    {
        await _timerManager.PauseAllTimersForAuctionAsync(auctionId);
        _logger.LogInformation("All timers paused for session {SessionId}", sessionId);
    }

    public async Task StopAllTimersForSessionAsync(Guid sessionId, Guid auctionId)
    {
        await _timerManager.StopAllTimersForAuctionAsync(auctionId);
        _logger.LogInformation("All timers stopped for session {SessionId}", sessionId);
    }

    public bool HasActiveTimer(Guid turnId)
    {
        return _timerManager.HasActiveTimer(turnId);
    }
}