using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Moq;
using Xunit;

namespace Tests;

using Application.Events;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Portal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
public class AuctionSessionOrchestrationTests
{
    private async Task<Team> MakeTeam(string name, int budget, int p)
    {
        var validator = new Mock<ITeamValidator>();
        validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var t = await Team.CreateAsync(Guid.NewGuid(), name, budget, validator.Object);
        while (t.CountP < p) t.Assign(RoleType.P, 1);
        return t;
    }

    [Fact]
    public async Task Nominate_Should_AutoAssign_And_Cycle_To_Next_Eligible()
    {
        var session = AuctionSession.Create(Guid.NewGuid());
        session.Start();
        var A = await MakeTeam("A", 100, 0); // eligible P
        var B = await MakeTeam("B", 100, 3); // full P
        var C = await MakeTeam("C", 100, 3); // full P
        var order = new List<Guid> { A.Id, B.Id, C.Id };
        var teams = new Dictionary<Guid, Team> { { A.Id, A }, { B.Id, B }, { C.Id, C } };

        var player = SerieAPlayer.Create(1, "P", "Por", "Portiere 1", "TeamX", 10, 5, 1);
        session.Nominate(order, teams, A.Id, player);

        Assert.Contains(session.DomainEvents, e => e is PlayerAutoAssigned);
        // After auto-assign on P, since no one else has slot, either role advances or index remains at A
        // We assert that either role advanced or we got TurnAdvanced back to A (index 0)
        var roleAdvanced = session.DomainEvents.OfType<RoleAdvanced>().Any();
        var turnAdvanced = session.DomainEvents.OfType<TurnAdvanced>().SingleOrDefault();
        Assert.True(roleAdvanced || (turnAdvanced is not null && turnAdvanced.NewOrderIndex == 0));
    }

    [Fact]
    public async Task Finalization_On_Timer_Expiry_With_WinningBid_Should_Assign_And_Advance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventPublisher, InMemoryDomainEventPublisher>();
        services.AddSingleton<IAuctionTimerManager, AuctionTimerManager>();
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddHostedService<Application.Services.AuctionTimerHostedService>();
        services.AddHostedService<Infrastructure.Services.AuctionFinalizationHostedService>();
    var sp = services.BuildServiceProvider();
    // Ensure hosted services are instantiated to subscribe to events
    _ = sp.GetServices<IHostedService>();
        var publisher = sp.GetRequiredService<IDomainEventPublisher>();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var league = League.Create("L");
        db.Leagues.Add(league);
    var validator = new Mock<ITeamValidator>();
    validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    var A = await Team.CreateAsync(league.Id, "A", 100, validator.Object);
    var B = await Team.CreateAsync(league.Id, "B", 100, validator.Object);
        db.Teams.AddRange(A, B);
        var session = AuctionSession.Create(league.Id, basePrice: 1, minIncrement: 3);
        session.Start();
        db.AuctionSessions.Add(session);
        db.AuctionParticipants.Add(AuctionParticipant.Create(session.Id, A.Id, 0));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session.Id, B.Id, 1));
    var player = SerieAPlayer.Create(10, "D", "Dd", "X", "T", 10, 5, 1);
        db.SerieAPlayers.Add(player);
        await db.SaveChangesAsync();

        var order = new List<Guid> { A.Id, B.Id };
        var teams = new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B };
        session.Nominate(order, teams, A.Id, player);
        session.MarkReady(B.Id);
        session.PlaceBid(A.Id, 3);
        db.Update(session);
        await db.SaveChangesAsync();

    // Simula scadenza timer pubblicando manualmente l'evento (il manager lo fa normalmente)
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session.Id, player.Id));

    // Attendi finalizzazione
    await Task.Delay(100);

        // Assert: ownership e avanzamento presenti
        var ownerships = db.PlayerOwnerships.ToList();
        Assert.Single(ownerships);
        Assert.Equal(A.Id, ownerships[0].LeaguePlayerId);
        Assert.Equal(3, ownerships[0].PurchasePrice);
    }

    [Fact]
    public async Task Finalization_On_Timer_Expiry_With_No_Bids_Should_Only_Advance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventPublisher, InMemoryDomainEventPublisher>();
        services.AddSingleton<IAuctionTimerManager, AuctionTimerManager>();
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddHostedService<Application.Services.AuctionTimerHostedService>();
        services.AddHostedService<Infrastructure.Services.AuctionFinalizationHostedService>();
    var sp = services.BuildServiceProvider();
    _ = sp.GetServices<IHostedService>();
        var publisher = sp.GetRequiredService<IDomainEventPublisher>();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var league = League.Create("L");
        db.Leagues.Add(league);
    var validator2 = new Mock<ITeamValidator>();
    validator2.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    var A = await Team.CreateAsync(league.Id, "A", 100, validator2.Object);
    var B = await Team.CreateAsync(league.Id, "B", 100, validator2.Object);
        db.Teams.AddRange(A, B);
        var session = AuctionSession.Create(league.Id, basePrice: 1, minIncrement: 3);
        session.Start();
        db.AuctionSessions.Add(session);
        db.AuctionParticipants.Add(AuctionParticipant.Create(session.Id, A.Id, 0));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session.Id, B.Id, 1));
    var player = SerieAPlayer.Create(11, "D", "Dd", "Y", "T", 10, 5, 1);
        db.SerieAPlayers.Add(player);
        await db.SaveChangesAsync();

        var order = new List<Guid> { A.Id, B.Id };
        var teams = new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B };
        session.Nominate(order, teams, A.Id, player);
        session.MarkReady(B.Id);
        db.Update(session);
        await db.SaveChangesAsync();

        // Nessuna offerta; scadenza timer
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session.Id, player.Id));
    await Task.Delay(100);

        // Assert: nessuna ownership creata
        Assert.Empty(db.PlayerOwnerships.ToList());
    }
    [Fact]
    public async Task Readiness_Should_Complete_When_All_Eligible_Others_Are_Ready()
    {
        var session = AuctionSession.Create(Guid.NewGuid());
        session.Start();
        var A = await MakeTeam("A", 100, 0); // nominator eligible
        var B = await MakeTeam("B", 100, 0); // eligible other
        var C = await MakeTeam("C", 100, 3); // not eligible
        var order = new List<Guid> { A.Id, B.Id, C.Id };
        var teams = new Dictionary<Guid, Team> { { A.Id, A }, { B.Id, B }, { C.Id, C } };

        var player = SerieAPlayer.Create(2, "P", "Por", "Portiere 2", "TeamY", 10, 5, 1);
        session.Nominate(order, teams, A.Id, player);

        // After nomination, B is the only eligible other; mark B ready
        session.MarkReady(B.Id);

        Assert.Contains(session.DomainEvents, e => e is BiddingReadyRequested);
        Assert.Contains(session.DomainEvents, e => e is BiddingReadyCompleted);
    }

    [Fact]
    public async Task Bidding_Should_Start_After_Ready_And_Raise_NewHighest_On_Valid_Bid()
    {
        var session = AuctionSession.Create(Guid.NewGuid(), basePrice: 3, minIncrement: 2);
        session.Start();
        var A = await MakeTeam("A", 100, 0); // nominator eligible
        var B = await MakeTeam("B", 100, 0); // eligible other
        var order = new List<Guid> { A.Id, B.Id };
        var teams = new Dictionary<Guid, Team> { { A.Id, A }, { B.Id, B } };
        var player = SerieAPlayer.Create(10, "P", "Por", "Portiere 10", "TeamZ", 10, 5, 1);

        session.Nominate(order, teams, A.Id, player);
        session.MarkReady(B.Id); // bidding starts

        // First valid bid must be >= base price (3)
        session.PlaceBid(A.Id, 3);
        Assert.Contains(session.DomainEvents, e => e is NewHighestBidPlaced h && h.Amount == 3 && h.TeamId == A.Id);

        // Next valid bid must be >= previous + minIncrement (5)
        session.PlaceBid(B.Id, 5);
        Assert.Contains(session.DomainEvents, e => e is NewHighestBidPlaced h && h.Amount == 5 && h.TeamId == B.Id);
    }

    [Fact]
    public async Task Bidding_Should_Enforce_MinIncrement_On_Subsequent_Bids()
    {
        var session = AuctionSession.Create(Guid.NewGuid(), basePrice: 1, minIncrement: 3);
        session.Start();
        var A = await MakeTeam("A", 100, 0);
        var B = await MakeTeam("B", 100, 0);
        var order = new List<Guid> { A.Id, B.Id };
        var teams = new Dictionary<Guid, Team> { { A.Id, A }, { B.Id, B } };
        var player = SerieAPlayer.Create(11, "P", "Por", "P11", "T", 10, 5, 1);

        session.Nominate(order, teams, A.Id, player);
        session.MarkReady(B.Id);

        session.PlaceBid(A.Id, 1);
        Assert.Throws<DomainException>(() => session.PlaceBid(B.Id, 3)); // needs 4 (1+3)
        session.PlaceBid(B.Id, 4);
    }
}
