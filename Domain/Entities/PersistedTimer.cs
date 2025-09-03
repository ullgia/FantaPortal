using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

public class PersistedTimer : BaseEntity
{
    public Guid TurnId { get; private set; }
    public Guid AuctionId { get; private set; }
    public Guid? LeagueId { get; private set; }
    public int? SerieAPlayerId { get; private set; }
    public Guid? SessionId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int InitialSeconds { get; private set; }
    public int WarningAtSeconds { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsPaused { get; private set; }
    public DateTime? PausedAt { get; private set; }
    public int PausedTotalSeconds { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    private PersistedTimer() { }

    public static PersistedTimer Create(
        Guid turnId,
        Guid auctionId,
        int initialSeconds,
        int warningAtSeconds = 10,
        Guid? leagueId = null,
        int? serieAPlayerId = null,
        Guid? sessionId = null)
    {
        if (turnId == Guid.Empty) throw new DomainException("TurnId required");
        if (auctionId == Guid.Empty) throw new DomainException("AuctionId required");
        if (initialSeconds <= 0) throw new DomainException("Initial seconds must be positive");

        var now = DateTime.UtcNow;
        return new PersistedTimer
        {
            TurnId = turnId,
            AuctionId = auctionId,
            LeagueId = leagueId,
            SerieAPlayerId = serieAPlayerId,
            SessionId = sessionId,
            StartedAt = now,
            ExpiresAt = now.AddSeconds(initialSeconds),
            InitialSeconds = initialSeconds,
            WarningAtSeconds = warningAtSeconds,
            IsActive = true,
            IsPaused = false,
            PausedTotalSeconds = 0
        };
    }

    public int GetRemainingSeconds()
    {
        if (!IsActive) return 0;
        if (IsPaused) return Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds);
        
        var remaining = (int)(ExpiresAt - DateTime.UtcNow).TotalSeconds;
        return Math.Max(0, remaining);
    }

    public bool HasExpired()
    {
        return IsActive && !IsPaused && DateTime.UtcNow >= ExpiresAt;
    }

    public void Pause()
    {
        if (!IsActive || IsPaused) return;
        
        IsPaused = true;
        PausedAt = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }

    public void Resume()
    {
        if (!IsActive || !IsPaused || !PausedAt.HasValue) return;

        var pausedDuration = (int)(DateTime.UtcNow - PausedAt.Value).TotalSeconds;
        PausedTotalSeconds += pausedDuration;
        ExpiresAt = ExpiresAt.AddSeconds(pausedDuration);
        
        IsPaused = false;
        PausedAt = null;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateRemainingSeconds(int newRemainingSeconds)
    {
        if (!IsActive) return;
        if (newRemainingSeconds < 0) throw new DomainException("Remaining seconds cannot be negative");

        ExpiresAt = DateTime.UtcNow.AddSeconds(newRemainingSeconds);
        LastUpdated = DateTime.UtcNow;
    }

    public void Stop()
    {
        IsActive = false;
        LastUpdated = DateTime.UtcNow;
    }

    public static List<PersistedTimer> GetExpiredTimers(IEnumerable<PersistedTimer> timers)
    {
        return timers.Where(t => t.HasExpired()).ToList();
    }

    public static List<PersistedTimer> GetActiveTimers(IEnumerable<PersistedTimer> timers)
    {
        return timers.Where(t => t.IsActive && !t.HasExpired()).ToList();
    }
}
