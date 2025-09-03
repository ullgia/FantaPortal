namespace Domain.Entities;

using Domain.Common;
using Domain.Exceptions;

public class PlayerOwnership : BaseEntity
{
    public Guid LeaguePlayerId { get; private set; }
    public int SerieAPlayerId { get; private set; }
    public int PurchasePrice { get; private set; }
    public DateTime AcquiredAt { get; private set; } = DateTime.UtcNow;
    public Guid AuctionEventId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public virtual Team Owner { get; private set; } = default!;
    public virtual SerieAPlayer Player { get; private set; } = default!;
    // AuctionEvent placeholder type is Bid for now (winning bid id) or a future AuctionEvent entity

    private PlayerOwnership() { }

    public static PlayerOwnership Create(Team owner, SerieAPlayer player, Guid auctionEventId, int price)
    {
        if (owner is null) throw new DomainException("Owner required");
        if (player is null) throw new DomainException("Player required");
        if (auctionEventId == Guid.Empty) throw new DomainException("Auction event required");
        if (price < 0) throw new DomainException("Price must be non-negative");

        return new PlayerOwnership
        {
            LeaguePlayerId = owner.Id,
            Owner = owner,
            SerieAPlayerId = player.Id,
            Player = player,
            AuctionEventId = auctionEventId,
            PurchasePrice = price,
            IsActive = true,
            AcquiredAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
    }
}
