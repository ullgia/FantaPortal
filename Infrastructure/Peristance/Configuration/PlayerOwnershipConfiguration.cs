using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class PlayerOwnershipConfiguration : IEntityTypeConfiguration<PlayerOwnership>
{
    public void Configure(EntityTypeBuilder<PlayerOwnership> b)
    {
        b.HasKey(o => o.Id);
        b.HasOne(o => o.Owner).WithMany().HasForeignKey(o => o.LeaguePlayerId);
        b.HasOne(o => o.Player).WithMany(nameof(SerieAPlayer.Ownerships)).HasForeignKey(o => o.SerieAPlayerId);
        b.Property(o => o.PurchasePrice).IsRequired();
        b.HasIndex(o => new { o.LeaguePlayerId, o.SerieAPlayerId, o.IsActive }).HasDatabaseName("IX_Ownership_UniqueActive");
    }
}
