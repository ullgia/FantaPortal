using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Persistence.Configuration;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.EventData)
            .IsRequired();
            
        builder.Property(x => x.Error)
            .HasMaxLength(1000);
            
        builder.Property(x => x.ProcessedBy)
            .HasMaxLength(250);
            
        builder.HasIndex(x => x.Processed);
        builder.HasIndex(x => x.NextRetryAt);
        builder.HasIndex(x => new { x.Processed, x.NextRetryAt });
        builder.HasIndex(x => x.CreatedAt);
    }
}
