namespace Domain.Entities;

using Domain.Common;

public class AuctionTurnOrder : BaseEntity
{
    public Guid AuctionSessionId { get; private set; }
    public Guid TeamId { get; private set; }
    public int Position { get; private set; }

    // Navigation properties
    public AuctionSession AuctionSession { get; private set; } = null!;
    public Team Team { get; private set; } = null!;

    private AuctionTurnOrder() { }

    internal static AuctionTurnOrder Create(Guid auctionSessionId, Guid teamId, int position)
    {
        return new AuctionTurnOrder
        {
            AuctionSessionId = auctionSessionId,
            TeamId = teamId,
            Position = position
        };
    }
}
