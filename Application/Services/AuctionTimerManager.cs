using System.Collections.Concurrent;
using Application.Events;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class AuctionTimerManager : IAuctionTimerManager, IDisposable
{
    private readonly ConcurrentDictionary<Guid, TimerWorker> _activeTimers = new();
    private readonly IAuctionTimerDataService _dataService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AuctionTimerManager> _logger;
    private bool _isDisposed;

    public AuctionTimerManager(
        IAuctionTimerDataService dataService,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AuctionTimerManager> logger)
    {
        _dataService = dataService;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task StartTimerAsync(Guid turnId, Guid auctionId, int remainingSeconds, Guid? leagueId = null, int warningAtSeconds = 30, int? serieAPlayerId = null, Guid? sessionId = null)
    {
        if (_isDisposed) return;

        await StopTimerAsync(turnId);

        var timer = PersistedTimer.Create(
            turnId,
            auctionId,
            remainingSeconds,
            warningAtSeconds,
            leagueId,
            serieAPlayerId,
            sessionId);

        await _dataService.SaveTimerAsync(timer);

        var worker = new TimerWorker(
            turnId,
            auctionId,
            leagueId ?? Guid.Empty,
            warningAtSeconds,
            remainingSeconds,
            _serviceScopeFactory,
            _logger,
            OnTimerExpiredAsync);

        _activeTimers.TryAdd(turnId, worker);
        await worker.StartAsync();

        _logger.LogInformation("Timer started for turn {TurnId} with {RemainingSeconds} seconds", turnId, remainingSeconds);
    }

    public async Task StopTimerAsync(Guid turnId)
    {
        if (_activeTimers.TryRemove(turnId, out var worker))
        {
            await worker.StopAsync();
            worker.Dispose();
        }

        var timer = await _dataService.GetTimerAsync(turnId);
        if (timer != null)
        {
            timer.Stop();
            await _dataService.SaveTimerAsync(timer);
        }

        _logger.LogInformation("Timer stopped for turn {TurnId}", turnId);
    }

    public async Task PauseTimerAsync(Guid turnId)
    {
        if (_activeTimers.TryGetValue(turnId, out var worker))
        {
            await worker.PauseAsync();
            
            var timer = await _dataService.GetTimerAsync(turnId);
            if (timer != null)
            {
                timer.Pause();
                await _dataService.SaveTimerAsync(timer);
            }
            
            _logger.LogInformation("Timer paused for turn {TurnId}", turnId);
        }
    }

    public async Task ResumeTimerAsync(Guid turnId)
    {
        if (_activeTimers.TryGetValue(turnId, out var worker))
        {
            await worker.ResumeAsync();
            
            var timer = await _dataService.GetTimerAsync(turnId);
            if (timer != null)
            {
                timer.Resume();
                await _dataService.SaveTimerAsync(timer);
            }
            
            _logger.LogInformation("Timer resumed for turn {TurnId}", turnId);
        }
    }

    public async Task UpdateTimerAsync(Guid turnId, int newRemainingSeconds)
    {
        if (_activeTimers.TryGetValue(turnId, out var worker))
        {
            await worker.UpdateRemainingSecondsAsync(newRemainingSeconds);
            
            var timer = await _dataService.GetTimerAsync(turnId);
            if (timer != null)
            {
                timer.UpdateRemainingSeconds(newRemainingSeconds);
                await _dataService.SaveTimerAsync(timer);
            }
            
            _logger.LogInformation("Timer updated for turn {TurnId} to {RemainingSeconds} seconds", turnId, newRemainingSeconds);
        }
    }

    public async Task PauseAllTimersForAuctionAsync(Guid auctionId)
    {
        var timers = await _dataService.GetTimersForAuctionAsync(auctionId);
        var tasks = timers.Select(t => PauseTimerAsync(t.TurnId));
        await Task.WhenAll(tasks);
        _logger.LogInformation("All timers paused for auction {AuctionId}", auctionId);
    }

    public async Task StopAllTimersForAuctionAsync(Guid auctionId)
    {
        var timers = await _dataService.GetTimersForAuctionAsync(auctionId);
        var tasks = timers.Select(t => StopTimerAsync(t.TurnId));
        await Task.WhenAll(tasks);
        _logger.LogInformation("All timers stopped for auction {AuctionId}", auctionId);
    }

    public bool HasActiveTimer(Guid turnId)
    {
        return _activeTimers.ContainsKey(turnId);
    }

    private async Task OnTimerExpiredAsync(Guid turnId)
    {
        try
        {
            if (_activeTimers.TryRemove(turnId, out var worker))
            {
                worker.Dispose();
            }

            var timer = await _dataService.GetTimerAsync(turnId);
            if (timer != null)
            {
                timer.Stop();
                await _dataService.SaveTimerAsync(timer);

                using var scope = _serviceScopeFactory.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
                
                // Create the event with required parameters
                var timerEvent = new BiddingTimerExpired(
                    turnId,
                    timer.SessionId ?? Guid.Empty,
                    timer.LeagueId ?? Guid.Empty,
                    timer.SerieAPlayerId ?? 0);
                    
                publisher.Publish(timerEvent);
            }

            _logger.LogInformation("Timer expired for turn {TurnId}", turnId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling timer expiration for turn {TurnId}", turnId);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (var worker in _activeTimers.Values)
        {
            worker.Dispose();
        }
        _activeTimers.Clear();
    }
}