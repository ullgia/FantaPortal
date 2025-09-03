using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.FirstName).HasMaxLength(100);
        b.Property(p => p.LastName).HasMaxLength(100);
        b.Property(p => p.TeamName).HasMaxLength(100);
    }
}
