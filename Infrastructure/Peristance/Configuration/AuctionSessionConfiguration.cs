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
        builder.HasOne<League>().WithMany().HasForeignKey(a => a.LeagueId);

    // Persist current bidding state via properties to allow hydration
    builder.Property(a => a.CurrentHighestBid).HasColumnName("CurrentHighestBid");
    builder.Property(a => a.CurrentHighestTeamId).HasColumnName("CurrentHighestTeamId");
    builder.Property(a => a.CurrentSerieAPlayerId).HasColumnName("CurrentSerieAPlayerId");
    builder.Property(a => a.IsBiddingActive).HasColumnName("IsBiddingActive");

        // Ignore transient readiness collections
    builder.Ignore("_eligibleForCurrentNomination");
    builder.Ignore("_readyTeamsForCurrentNomination");
    }
}
