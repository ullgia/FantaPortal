namespace Domain.Entities;

using Domain.Common;
using Domain.Exceptions;

public class PlayerOwnership : BaseEntity
{
    public Guid TeamId { get; private set; }
    public int SerieAPlayerId { get; private set; }
    public int PurchasePrice { get; private set; }
    public DateTime AcquiredAt { get; private set; } = DateTime.UtcNow;
    public Guid AuctionSessionId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? DeactivationReason { get; private set; }

    // Navigation property per SerieAPlayer
    public SerieAPlayer SerieAPlayer { get; private set; } = default!;

    private PlayerOwnership() { }

    internal static PlayerOwnership CreateInternal(Guid teamId, SerieAPlayer player, int price, Guid auctionSessionId)
    {
        if (teamId == Guid.Empty) throw new DomainException("TeamId required");
        if (player == null) throw new DomainException("SerieAPlayer required");
        if (price < 0) throw new DomainException("Price must be non-negative");
        if (auctionSessionId == Guid.Empty) throw new DomainException("AuctionSessionId required");

        return new PlayerOwnership
        {
            TeamId = teamId,
            SerieAPlayerId = player.Id,
            SerieAPlayer = player,
            PurchasePrice = price,
            AuctionSessionId = auctionSessionId,
            IsActive = true,
            AcquiredAt = DateTime.UtcNow
        };
    }

    internal void DeactivateInternal(string reason)
    {
        if (!IsActive) return;
        IsActive = false;
        DeactivationReason = reason;
    }
}
