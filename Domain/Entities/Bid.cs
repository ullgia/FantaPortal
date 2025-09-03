namespace Domain.Entities;

using Domain.Common;
using Domain.Exceptions;

public class Bid : BaseEntity
{
    public Guid TurnId { get; private set; }
    public Guid TeamId { get; private set; }
    public int Amount { get; private set; }
    public DateTime PlacedAt { get; private set; } = DateTime.UtcNow;

    private Bid() { }

    public static Bid Create(Guid turnId, Guid teamId, int amount)
    {
        if (turnId == Guid.Empty) throw new DomainException("TurnId required");
        if (teamId == Guid.Empty) throw new DomainException("TeamId required");
        if (amount <= 0) throw new DomainException("Amount must be positive");
        return new Bid
        {
            TurnId = turnId,
            TeamId = teamId,
            Amount = amount
        };
    }
}
