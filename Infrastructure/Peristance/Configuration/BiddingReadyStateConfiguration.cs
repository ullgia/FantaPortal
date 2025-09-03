namespace Infrastructure.Peristance.Configuration;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BiddingReadyStateConfiguration : IEntityTypeConfiguration<BiddingReadyState>
{
    public void Configure(EntityTypeBuilder<BiddingReadyState> builder)
    {
        builder.ToTable("BiddingReadyStates");
        
        builder.HasKey(x => x.Id);
        
        // Proprietà base
        builder.Property(x => x.SessionId)
            .IsRequired();
            
        builder.Property(x => x.NominatorTeamId)
            .IsRequired();
            
        builder.Property(x => x.SerieAPlayerId)
            .IsRequired();
            
        builder.Property(x => x.Role)
            .HasConversion<string>()
            .IsRequired();
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.IsCompleted)
            .IsRequired();

        // Lista EligibleTeamIds come stringa JSON
        builder.Property(x => x.EligibleTeamIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)!.AsReadOnly())
            .HasColumnType("text");

        // Lista ReadyTeamIds come stringa JSON (proprietà calcolata)
        builder.Property(x => x.ReadyTeamIdsForPersistence)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnName("ReadyTeamIds")
            .HasColumnType("text");

        // Relazione con AuctionSession
        builder.HasOne<AuctionSession>()
            .WithMany(x => x.ReadyStates)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indici per performance
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => new { x.SessionId, x.IsCompleted });
    }
}
