using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> b)
    {
        b.HasKey(bd => bd.Id);
        b.HasOne<AuctionTurn>().WithMany().HasForeignKey(bd => bd.TurnId);
        b.HasOne<LeaguePlayer>().WithMany().HasForeignKey(bd => bd.TeamId);
    }
}
