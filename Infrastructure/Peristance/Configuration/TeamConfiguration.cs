using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Name).HasMaxLength(100);
        b.HasOne<League>().WithMany().HasForeignKey(t => t.LeagueId);
        b.HasIndex(t => new { t.LeagueId, t.Name }).IsUnique();
    }
}
