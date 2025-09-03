using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Persistence.Configuration;

public class PersistedTimerConfiguration : IEntityTypeConfiguration<PersistedTimer>
{
    public void Configure(EntityTypeBuilder<PersistedTimer> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.TurnId)
            .IsRequired();
            
        builder.Property(x => x.AuctionId)
            .IsRequired();
            
        builder.Property(x => x.InitialSeconds)
            .IsRequired();
            
        builder.Property(x => x.WarningAtSeconds)
            .IsRequired();
            
        builder.HasIndex(x => x.TurnId)
            .IsUnique();
            
        builder.HasIndex(x => x.AuctionId);
        builder.HasIndex(x => new { x.IsActive, x.ExpiresAt });
        builder.HasIndex(x => new { x.IsActive, x.IsPaused });
    }
}
