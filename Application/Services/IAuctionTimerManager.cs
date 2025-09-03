namespace Application.Services;

public interface IAuctionTimerManager
{
    Task StartTimerAsync(Guid turnId, Guid auctionId, int remainingSeconds, Guid? leagueId = null, int warningAtSeconds = 30, int? serieAPlayerId = null, Guid? sessionId = null);
    Task StopTimerAsync(Guid turnId);
    Task PauseTimerAsync(Guid turnId);
    Task ResumeTimerAsync(Guid turnId);
    Task UpdateTimerAsync(Guid turnId, int newRemainingSeconds);
    Task PauseAllTimersForAuctionAsync(Guid auctionId);
    Task StopAllTimersForAuctionAsync(Guid auctionId);
    bool HasActiveTimer(Guid turnId);
    
    // Bidding-specific timer management
    Task StartBiddingTimerAsync(Guid sessionId, int durationSeconds);
    Task StopBiddingTimerAsync(Guid sessionId);
}
