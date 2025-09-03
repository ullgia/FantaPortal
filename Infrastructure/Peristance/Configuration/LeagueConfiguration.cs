using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);

        // Teams collection - League aggregate owns them
        builder.HasMany(l => l.Teams)
            .WithOne()
            .HasForeignKey("LeagueId")
            .OnDelete(DeleteBehavior.Cascade);

        // PlayerOwnerships collection - League aggregate owns them  
        builder.HasMany(l => l.PlayerOwnerships)
            .WithOne()
            .HasForeignKey("LeagueId")
            .OnDelete(DeleteBehavior.Cascade);

        // ActiveAuction as owned entity (one-to-one)
        builder.HasOne(l => l.ActiveAuction)
            .WithOne()
            .HasForeignKey<AuctionSession>("LeagueId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
