using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class AuctionSessionConfiguration : IEntityTypeConfiguration<AuctionSession>
{
    public void Configure(EntityTypeBuilder<AuctionSession> builder)
    {
        // Keys and relationships
        builder.HasKey(a => a.Id);
        builder.Property(a => a.LeagueId).IsRequired();

        // Basic auction properties
        builder.Property(a => a.Status).IsRequired();
        builder.Property(a => a.CurrentRole).IsRequired();
        builder.Property(a => a.BasePrice).IsRequired();
        builder.Property(a => a.MinIncrement).IsRequired();

        // Persist current bidding state
        builder.Property(a => a.IsBiddingActive).HasColumnName("IsBiddingActive");
        builder.Property(a => a.CurrentSerieAPlayerId).HasColumnName("CurrentSerieAPlayerId");

        // Configure TurnOrders relationship
        builder.HasMany(a => a.TurnOrders)
            .WithOne(to => to.AuctionSession)
            .HasForeignKey(to => to.AuctionSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore internal bidding state
        builder.Ignore("_currentBidding");
    }
}
