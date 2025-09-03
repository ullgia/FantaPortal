using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Reflection;
using Application.Events;
using Domain.Common;

namespace Portal.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDomainEventPublisher publisher) : IdentityDbContext<ApplicationUser>(options)
    {
    private readonly IDomainEventPublisher _publisher = publisher;
    public DbSet<Player> Players => Set<Player>();
        public DbSet<League> Leagues => Set<League>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<AuctionSession> AuctionSessions => Set<AuctionSession>();
        public DbSet<AuctionTurn> AuctionTurns => Set<AuctionTurn>();
        public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<SerieAPlayer> SerieAPlayers => Set<SerieAPlayer>();
    public DbSet<PlayerOwnership> PlayerOwnerships => Set<PlayerOwnership>();
    public DbSet<AuctionParticipant> AuctionParticipants => Set<AuctionParticipant>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public override int SaveChanges()
        {
            var result = base.SaveChanges();
            DispatchDomainEvents();
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            DispatchDomainEvents();
            return result;
        }

        private void DispatchDomainEvents()
        {
            var aggregates = ChangeTracker.Entries()
                .Select(e => e.Entity)
                .OfType<AggregateRoot>()
                .ToList();

            foreach (var agg in aggregates)
            {
                foreach (var @event in agg.DomainEvents)
                    _publisher.Publish(@event);
                agg.ClearDomainEvents();
            }
        }
    }
}
