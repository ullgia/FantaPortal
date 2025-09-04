using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Peristance.Configuration;

public class AuctionParticipantConfiguration : IEntityTypeConfiguration<AuctionParticipant>
{
    public void Configure(EntityTypeBuilder<AuctionParticipant> b)
    {
        b.HasKey(p => p.Id);
        b.HasOne<AuctionSession>().WithMany().HasForeignKey(p => p.SessionId);
        b.HasOne<LeaguePlayer>().WithMany().HasForeignKey(p => p.TeamId);
        b.HasIndex(p => new { p.SessionId, p.OrderIndex }).IsUnique();
    }
}
