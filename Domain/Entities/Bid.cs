namespace Domain.Entities;

using Domain.Common;
using Domain.Exceptions;

public class Bid : BaseEntity
{
    public Guid AuctionTurnId { get; private set; }
    public Guid LeaguePlayerId { get; private set; }
    public int SerieAPlayerId { get; private set; }
    public int Amount { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public bool IsWinningBid { get; internal set; }

    public virtual AuctionTurn AuctionTurn { get; private set; } = default!;
    public virtual LeaguePlayer Bidder { get; private set; } = default!;
    public virtual SerieAPlayer TargetPlayer { get; private set; } = default!;

    public static Bid Create(AuctionTurn turn, LeaguePlayer bidder, SerieAPlayer target, int amount)
    {
        if (turn is null) throw new DomainException("turn required");
        if (bidder is null) throw new DomainException("bidder required");
        if (target is null) throw new DomainException("target required");
        if (amount <= 0) throw new DomainException("amount must be positive");
        return new Bid
        {
            AuctionTurn = turn,
            AuctionTurnId = turn.Id,
            Bidder = bidder,
            LeaguePlayerId = bidder.Id,
            TargetPlayer = target,
            SerieAPlayerId = target.Id,
            Amount = amount,
            Timestamp = DateTime.UtcNow,
            IsWinningBid = false
        };
    }
}
