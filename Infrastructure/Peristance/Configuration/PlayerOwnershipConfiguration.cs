using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class PlayerOwnershipConfiguration : IEntityTypeConfiguration<PlayerOwnership>
{
    public void Configure(EntityTypeBuilder<PlayerOwnership> b)
    {
        b.HasKey(o => o.Id);
        
        // Basic properties
        b.Property(o => o.TeamId).IsRequired();
        b.Property(o => o.SerieAPlayerId).IsRequired();
        b.Property(o => o.PurchasePrice).IsRequired();
        b.Property(o => o.AcquiredAt).IsRequired();
        b.Property(o => o.IsActive).IsRequired();
        
        // Foreign key relationships (owned by League aggregate)
        b.HasOne<Team>().WithMany().HasForeignKey(o => o.TeamId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SerieAPlayer>().WithMany().HasForeignKey(o => o.SerieAPlayerId).OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        b.HasIndex(o => new { o.TeamId, o.SerieAPlayerId, o.IsActive }).HasDatabaseName("IX_Ownership_UniqueActive");
        b.HasIndex(o => o.AcquiredAt);
    }
}
