using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class SerieAPlayerConfiguration : IEntityTypeConfiguration<SerieAPlayer>
{
    public void Configure(EntityTypeBuilder<SerieAPlayer> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Role).HasMaxLength(2);
        b.Property(p => p.RoleExtended).HasMaxLength(10);
        b.Property(p => p.Name).HasMaxLength(120);
        b.Property(p => p.Team).HasMaxLength(60);
    }
}
