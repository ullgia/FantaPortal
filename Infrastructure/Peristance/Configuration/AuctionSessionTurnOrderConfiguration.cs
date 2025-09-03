using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class AuctionSessionTurnOrderConfiguration : IEntityTypeConfiguration<AuctionSessionTurnOrder>
{
    public void Configure(EntityTypeBuilder<AuctionSessionTurnOrder> builder)
    {
        builder.ToTable("AuctionSessionTurnOrders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AuctionSessionId)
            .IsRequired();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.Position)
            .IsRequired();

        // Foreign key relationships
        builder.HasOne(x => x.AuctionSession)
            .WithMany(x => x.TurnOrders)
            .HasForeignKey(x => x.AuctionSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Team)
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => new { x.AuctionSessionId, x.Position })
            .IsUnique();

        builder.HasIndex(x => new { x.AuctionSessionId, x.TeamId })
            .IsUnique();
    }
}
