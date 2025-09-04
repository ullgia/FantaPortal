namespace Application.Services;

public interface IAuctionTimerManager
{
    Task StartTimerAsync(Guid auctionId, int remainingSeconds, Guid? leagueId = null, int warningAtSeconds = 30, int? serieAPlayerId = null, Guid? sessionId = null);
    Task StopTimerAsync(Guid auctionId);
    Task PauseTimerAsync(Guid auctionId);
    Task ResumeTimerAsync(Guid auctionId);
    Task UpdateTimerAsync(Guid auctionId, int newRemainingSeconds);
    Task PauseAllTimersForAuctionAsync(Guid auctionId);
    Task StopAllTimersForAuctionAsync(Guid auctionId);
    bool HasActiveTimer(Guid auctionId);

    // Bidding-specific timer management
    Task StartBiddingTimerAsync(Guid auctionId, int durationSeconds);
    Task StopBiddingTimerAsync(Guid auctionId);
}
