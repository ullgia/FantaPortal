using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class LeaguePlayerConfiguration : IEntityTypeConfiguration<LeaguePlayer>
{
    public void Configure(EntityTypeBuilder<LeaguePlayer> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Name).HasMaxLength(100);
        b.Property(t => t.LeagueId).IsRequired();
        
        // Team belongs to League but is part of League aggregate
        b.HasOne<League>().WithMany().HasForeignKey(t => t.LeagueId);
        b.HasIndex(t => new { t.LeagueId, t.Name }).IsUnique();
    }
}
