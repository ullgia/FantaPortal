using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Reflection;

namespace Infrastructure.Peristance
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
    public DbSet<Player> Players => Set<Player>();
        public DbSet<League> Leagues => Set<League>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<AuctionSession> AuctionSessions => Set<AuctionSession>();
        public DbSet<AuctionSessionTurnOrder> AuctionSessionTurnOrders => Set<AuctionSessionTurnOrder>();
        public DbSet<BiddingReadyState> BiddingReadyStates => Set<BiddingReadyState>();
        public DbSet<AuctionTurn> AuctionTurns => Set<AuctionTurn>();
        public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<SerieAPlayer> SerieAPlayers => Set<SerieAPlayer>();
    public DbSet<PlayerOwnership> PlayerOwnerships => Set<PlayerOwnership>();
    public DbSet<AuctionParticipant> AuctionParticipants => Set<AuctionParticipant>();
    public DbSet<MagicLink> MagicLinks => Set<MagicLink>();
    public DbSet<PersistedTimer> PersistedTimers => Set<PersistedTimer>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public override int SaveChanges()
        {
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
