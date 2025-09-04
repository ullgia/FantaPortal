namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

public class AuctionTurn : BaseEntity
{
    public Guid SessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public virtual LeaguePlayer Player { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public AuctionTurnStatus Status { get; private set; } = AuctionTurnStatus.Nomination;
    public bool IsTimerActive { get; private set; }
    public DateTime? TimerStartedAt { get; private set; }
    public int RemainingSeconds { get; private set; }

    private AuctionTurn() { }

    public static AuctionTurn Create(Guid sessionId, Guid playerId)
    {
        if (sessionId == Guid.Empty) throw new DomainException("SessionId required");
        if (playerId == Guid.Empty) throw new DomainException("PlayerId required");
        return new AuctionTurn
        {
            SessionId = sessionId,
            PlayerId = playerId,
            Status = AuctionTurnStatus.Nomination,
            IsTimerActive = false,
            RemainingSeconds = 0
        };
    }

    public void StartTimer(int seconds)
    {
        if (seconds <= 0) throw new DomainException("Timer seconds must be positive");
        IsTimerActive = true;
        TimerStartedAt = DateTime.UtcNow;
        RemainingSeconds = seconds;
    }

    public void PauseTimer()
    {
        IsTimerActive = false;
    }

    public void ResumeTimer()
    {
        IsTimerActive = true;
        TimerStartedAt = DateTime.UtcNow;
    }

    public void SetStatus(AuctionTurnStatus status) => Status = status;
}
