namespace Domain.Entities;

using Domain.Common;
using Domain.Exceptions;

public class AuctionParticipant : BaseEntity
{
    public Guid SessionId { get; private set; }
    public Guid TeamId { get; private set; }
    public int OrderIndex { get; private set; }

    private AuctionParticipant() { }

    public static AuctionParticipant Create(Guid sessionId, Guid teamId, int orderIndex)
    {
        if (sessionId == Guid.Empty) throw new DomainException("SessionId required");
        if (teamId == Guid.Empty) throw new DomainException("TeamId required");
        if (orderIndex < 0) throw new DomainException("OrderIndex must be non-negative");
        return new AuctionParticipant
        {
            SessionId = sessionId,
            TeamId = teamId,
            OrderIndex = orderIndex
        };
    }
}
