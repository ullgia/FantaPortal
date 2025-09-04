using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class AuctionTurnConfiguration : IEntityTypeConfiguration<AuctionTurn>
{
    public void Configure(EntityTypeBuilder<AuctionTurn> b)
    {
        b.HasKey(t => t.Id);
        b.HasOne<AuctionSession>().WithMany().HasForeignKey(t => t.SessionId);

        b.HasOne(p => p.Player).WithMany().HasForeignKey(p => p.PlayerId);

    }
}
