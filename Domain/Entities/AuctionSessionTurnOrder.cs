using Domain.Common;

namespace Domain.Entities;

public class AuctionSessionTurnOrder : BaseEntity<Guid>
{
    public Guid AuctionSessionId { get; private set; }
    public Guid TeamId { get; private set; }
    public int Position { get; private set; }

    // Navigation properties
    public AuctionSession AuctionSession { get; private set; } = null!;
    public LeaguePlayer Team { get; private set; } = null!;

    // Private constructor for EF
    private AuctionSessionTurnOrder() { }

    public AuctionSessionTurnOrder(Guid auctionSessionId, Guid teamId, int position)
    {
        Id = Guid.NewGuid();
        AuctionSessionId = auctionSessionId;
        TeamId = teamId;
        Position = position;
    }

    public static AuctionSessionTurnOrder Create(Guid auctionSessionId, Guid teamId, int position)
    {
        return new AuctionSessionTurnOrder(auctionSessionId, teamId, position);
    }
}
