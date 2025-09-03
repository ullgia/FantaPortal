using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Persistence.Configuration;

public class MagicLinkConfiguration : IEntityTypeConfiguration<MagicLink>
{
    public void Configure(EntityTypeBuilder<MagicLink> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.ParticipantName)
            .IsRequired()
            .HasMaxLength(250);
            
        builder.Property(x => x.UsedByIpAddress)
            .HasMaxLength(50);
            
        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();
            
        builder.HasIndex(x => x.Token)
            .IsUnique();
            
        builder.HasIndex(x => x.LeagueId);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasIndex(x => new { x.IsActive, x.IsUsed });
    }
}
