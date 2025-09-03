using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Application.Events;

namespace Application.Services;

public class AuctionTimerManager : IAuctionTimerManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionTimerManager> _logger;
    private readonly ConcurrentDictionary<Guid, TimerWorker> _workers = new();
    private readonly ConcurrentDictionary<Guid, (Guid AuctionId, Guid? LeagueId, int? SerieAPlayerId, Guid? SessionId)> _metadata = new();
    private readonly IDomainEventPublisher _publisher;

    public AuctionTimerManager(IServiceScopeFactory scopeFactory, ILogger<AuctionTimerManager> logger, IDomainEventPublisher publisher)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _publisher = publisher;
    }

    public bool HasActiveTimer(Guid turnId) => _workers.TryGetValue(turnId, out var w) && w.IsRunning;

    public Task StartTimerAsync(Guid turnId, Guid auctionId, int remainingSeconds, Guid? leagueId = null, int warningAtSeconds = 30, int? serieAPlayerId = null, Guid? sessionId = null)
    {
        _metadata[turnId] = (auctionId, leagueId, serieAPlayerId, sessionId);
        var worker = _workers.GetOrAdd(turnId, id => new TimerWorker(id, auctionId, leagueId ?? Guid.Empty, warningAtSeconds, remainingSeconds, _scopeFactory, _logger, OnExpiredAsync));
        return worker.StartAsync();
    }

    public Task StopTimerAsync(Guid turnId)
    {
        if (_workers.TryRemove(turnId, out var worker))
        {
            worker.Dispose();
        }
        return Task.CompletedTask;
    }

    public Task PauseTimerAsync(Guid turnId)
    {
        if (_workers.TryGetValue(turnId, out var w)) return w.PauseAsync();
        return Task.CompletedTask;
    }

    public Task ResumeTimerAsync(Guid turnId)
    {
        if (_workers.TryGetValue(turnId, out var w)) return w.ResumeAsync();
        return Task.CompletedTask;
    }

    public Task UpdateTimerAsync(Guid turnId, int newRemainingSeconds)
    {
        if (_workers.TryGetValue(turnId, out var w)) return w.UpdateRemainingSecondsAsync(newRemainingSeconds);
        return Task.CompletedTask;
    }

    public Task PauseAllTimersForAuctionAsync(Guid auctionId)
    {
        foreach (var w in _workers.Values.Where(x => x.AuctionId == auctionId))
            w.PauseAsync();
        return Task.CompletedTask;
    }

    public Task StopAllTimersForAuctionAsync(Guid auctionId)
    {
        foreach (var kv in _workers.Where(kv => kv.Value.AuctionId == auctionId))
            StopTimerAsync(kv.Key);
        return Task.CompletedTask;
    }

    private Task OnExpiredAsync(Guid turnId)
    {
        _logger.LogInformation("Timer expired for Turn {TurnId}", turnId);
        if (_metadata.TryGetValue(turnId, out var meta) && meta.SessionId.HasValue && meta.SerieAPlayerId.HasValue)
        {
            _publisher.Publish(new BiddingTimerExpired(turnId, meta.SessionId.Value, meta.SerieAPlayerId.Value));
        }
        return Task.CompletedTask;
    }
}
