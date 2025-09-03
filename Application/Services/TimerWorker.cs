using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class TimerWorker : IDisposable
{
    private readonly Guid _turnId;
    private readonly Guid _auctionId;
    private readonly Guid _leagueId;
    private readonly int _warningAtSeconds;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger _logger;
    private readonly Func<Guid, Task> _onTimerExpired;

    private readonly System.Timers.Timer _timer;
    private readonly object _lock = new();

    private int _remainingSeconds;
    private int _lastSentRemaining = -1;
    private int _lastWarningSecond = int.MinValue;
    private bool _isPaused;
    private bool _isDisposed;

    public Guid TurnId => _turnId;
    public Guid AuctionId => _auctionId;
    public bool IsRunning => _timer.Enabled && !_isPaused;
    public bool IsPaused => _isPaused;
    public int RemainingSeconds { get { lock (_lock) { return _remainingSeconds; } } }

    public TimerWorker(
        Guid turnId,
        Guid auctionId,
        Guid leagueId,
        int warningAtSeconds,
        int initialRemainingSeconds,
        IServiceScopeFactory serviceScopeFactory,
        ILogger logger,
        Func<Guid, Task> onTimerExpired)
    {
        _turnId = turnId;
        _auctionId = auctionId;
        _leagueId = leagueId;
        _warningAtSeconds = warningAtSeconds;
        _remainingSeconds = initialRemainingSeconds;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _onTimerExpired = onTimerExpired;

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    public Task StartAsync() { lock (_lock) { if (_isDisposed) return Task.CompletedTask; _timer.Start(); } return Task.CompletedTask; }
    public Task StopAsync() { lock (_lock) { if (_isDisposed) return Task.CompletedTask; _timer.Stop(); } return Task.CompletedTask; }
    public Task PauseAsync() { lock (_lock) { if (_isDisposed || _isPaused) return Task.CompletedTask; _isPaused = true; } return Task.CompletedTask; }
    public Task ResumeAsync() { lock (_lock) { if (_isDisposed || !_isPaused) return Task.CompletedTask; _isPaused = false; } return Task.CompletedTask; }
    public Task UpdateRemainingSecondsAsync(int newRemainingSeconds) { lock (_lock) { if (_isDisposed) return Task.CompletedTask; _remainingSeconds = newRemainingSeconds; } return Task.CompletedTask; }

    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            bool shouldFinalize = false;
            int currentRemaining;
            lock (_lock)
            {
                if (_isDisposed || _isPaused) return;
                _remainingSeconds = Math.Max(0, _remainingSeconds - 1);
                currentRemaining = _remainingSeconds;
                if (currentRemaining <= 0) { _timer.Stop(); shouldFinalize = true; }
            }
            await SendTimerNotificationsAsync(currentRemaining);
            if (shouldFinalize) await _onTimerExpired(_turnId);
        }
        catch (Exception)
        {
            lock (_lock) { _timer.Stop(); }
        }
    }

    private async Task SendTimerNotificationsAsync(int currentRemaining)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var signalR = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
            if (currentRemaining != _lastSentRemaining)
            {
                _lastSentRemaining = currentRemaining;
                await signalR.TimerUpdate(_leagueId, _auctionId, _turnId, currentRemaining);
                if (currentRemaining > 0 && currentRemaining <= _warningAtSeconds && currentRemaining != _lastWarningSecond && currentRemaining % 5 == 0)
                {
                    _lastWarningSecond = currentRemaining;
                    await signalR.TimerWarning(_leagueId, _auctionId, _turnId, currentRemaining);
                }
            }
        }
        catch { }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
