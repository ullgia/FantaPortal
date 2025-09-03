using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Events;
using Application.Services;
using Domain.Contracts;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Portal.Data;
using Xunit;
using Microsoft.Extensions.Hosting;

namespace Tests;

public class SeasonEndToEndTests
{
    private static async Task<Team> MakeTeamAsync(Guid leagueId, string name, int budget, ITeamValidator validator)
        => await Team.CreateAsync(leagueId, name, budget, validator);

    private static void FillRoster(Team t, int p, int d, int c, int a)
    {
        while (t.CountP < p) t.Assign(RoleType.P, 1);
        while (t.CountD < d) t.Assign(RoleType.D, 1);
        while (t.CountC < c) t.Assign(RoleType.C, 1);
        while (t.CountA < a) t.Assign(RoleType.A, 1);
    }

    [Fact]
    public async Task FullSeason_Flow_With_RepairAuctions_And_Releases_Works()
    {
        // Setup real services except realtime (NoOp) and Identity.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventPublisher, InMemoryDomainEventPublisher>();
        services.AddSingleton<IAuctionTimerManager, AuctionTimerManager>();
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddHostedService<AuctionTimerHostedService>();
        services.AddHostedService<Infrastructure.Services.AuctionFinalizationHostedService>();
        services.AddScoped<Application.Services.IRealtimeNotificationService, NoOpRealtimeNotificationService>();
        services.AddScoped<ITeamValidator, Infrastructure.Validators.TeamValidator>();
        var sp = services.BuildServiceProvider();
    // Ensure hosted services are instantiated to subscribe to events
    _ = sp.GetServices<IHostedService>();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        var teamValidator = scope.ServiceProvider.GetRequiredService<ITeamValidator>();

        // 1) Creazione Lega
        var league = League.Create("Serie A Fanta");
        db.Leagues.Add(league);

        // 2) Creazione squadre
        var A = await MakeTeamAsync(league.Id, "Alpha", 500, teamValidator);
        var B = await MakeTeamAsync(league.Id, "Beta", 500, teamValidator);
        var C = await MakeTeamAsync(league.Id, "Gamma", 500, teamValidator);
        db.Teams.AddRange(A, B, C);

        // 3) Import base giocatori usati nei test
        var p1 = SerieAPlayer.Create(101, "P", "Por", "P101", "T1", 10, 5, 1);
        var p2 = SerieAPlayer.Create(102, "D", "Dd", "P102", "T1", 10, 5, 1);
        var p3 = SerieAPlayer.Create(103, "C", "C", "P103", "T1", 10, 5, 1);
        var p4 = SerieAPlayer.Create(104, "A", "A", "P104", "T1", 10, 5, 1);
        db.SerieAPlayers.AddRange(p1, p2, p3, p4);
        await db.SaveChangesAsync();

        // 4) Prima auction: completa (riempie i ruoli base)
        var session1 = AuctionSession.Create(league.Id, basePrice: 1, minIncrement: 1);
        session1.Start();
        db.AuctionSessions.Add(session1);
        db.AuctionParticipants.Add(AuctionParticipant.Create(session1.Id, A.Id, 0));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session1.Id, B.Id, 1));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session1.Id, C.Id, 2));
        await db.SaveChangesAsync();

        // Nomina P: A nomina p1, B e C eligibili -> mark ready e offerte
        session1.Nominate(new List<Guid> { A.Id, B.Id, C.Id }, new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B, [C.Id] = C }, A.Id, p1);
        session1.MarkReady(B.Id);
        session1.MarkReady(C.Id);
        session1.PlaceBid(A.Id, 5);
        session1.PlaceBid(B.Id, 6);
        db.Update(session1); await db.SaveChangesAsync();
        // scadenza timer -> assegna a B
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session1.Id, p1.Id));
    await Task.Delay(120);

        // Nomina D: B nomina p2 -> A e C eligibili
        session1.Nominate(new List<Guid> { A.Id, B.Id, C.Id }, new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B, [C.Id] = C }, B.Id, p2);
        session1.MarkReady(A.Id);
        session1.MarkReady(C.Id);
        session1.PlaceBid(C.Id, 3);
        db.Update(session1); await db.SaveChangesAsync();
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session1.Id, p2.Id));
    await Task.Delay(120);

        // Nomina C: C nomina p3 -> tutti eligibili
        session1.Nominate(new List<Guid> { A.Id, B.Id, C.Id }, new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B, [C.Id] = C }, C.Id, p3);
        session1.MarkReady(A.Id);
        session1.MarkReady(B.Id);
        session1.PlaceBid(A.Id, 7);
        session1.PlaceBid(B.Id, 8);
        db.Update(session1); await db.SaveChangesAsync();
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session1.Id, p3.Id));
    await Task.Delay(120);

        // Nomina A: A nomina p4 -> tutti eligibili, nessuna offerta
        session1.Nominate(new List<Guid> { A.Id, B.Id, C.Id }, new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B, [C.Id] = C }, A.Id, p4);
        session1.MarkReady(B.Id);
        session1.MarkReady(C.Id);
        db.Update(session1); await db.SaveChangesAsync();
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session1.Id, p4.Id));
    await Task.Delay(120);

        // Verifiche post session1: alcune ownership create e conteggi ruoli coerenti
        var own1 = db.PlayerOwnerships.ToList();
        Assert.True(own1.Count >= 3);
        Assert.True(A.CountP + A.CountD + A.CountC + A.CountA > 0, $"Expected A to have at least one player, but found none.");
        Assert.True(B.CountP + B.CountD + B.CountC + B.CountA > 0, $"Expected B to have at least one player, but found none.");

        // 5) Seconda auction: gestione svincoli (tagli) e consultazione rosa
        // Simula svincoli: B svincola un C (se ne ha), A svincola un D
        if (B.CountC > 0) B.Release(RoleType.C, refund: 1);
        if (A.CountD > 0) A.Release(RoleType.D, refund: 1);
        db.UpdateRange(A, B); await db.SaveChangesAsync();

        var session2 = AuctionSession.Create(league.Id, basePrice: 1, minIncrement: 1);
        session2.Start();
        db.AuctionSessions.Add(session2);
        db.AuctionParticipants.Add(AuctionParticipant.Create(session2.Id, A.Id, 0));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session2.Id, B.Id, 1));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session2.Id, C.Id, 2));
        await db.SaveChangesAsync();

        // A nomina un nuovo C (p3) e tutti fanno ready; offerta minima di C
        session2.Nominate(new List<Guid> { A.Id, B.Id, C.Id }, new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B, [C.Id] = C }, A.Id, p3);
        session2.MarkReady(B.Id);
        session2.MarkReady(C.Id);
        session2.PlaceBid(C.Id, 2);
        db.Update(session2); await db.SaveChangesAsync();
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session2.Id, p3.Id));
    await Task.Delay(120);

        // 6) Terza auction di riparazione: parte da rose parziali
        var session3 = AuctionSession.Create(league.Id, basePrice: 1, minIncrement: 1);
        session3.Start();
        db.AuctionSessions.Add(session3);
        db.AuctionParticipants.Add(AuctionParticipant.Create(session3.Id, A.Id, 0));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session3.Id, B.Id, 1));
        db.AuctionParticipants.Add(AuctionParticipant.Create(session3.Id, C.Id, 2));
        await db.SaveChangesAsync();

        // B nomina un P (p1) e viene assegnato senza offerte
        session3.Nominate(new List<Guid> { A.Id, B.Id, C.Id }, new Dictionary<Guid, Team> { [A.Id] = A, [B.Id] = B, [C.Id] = C }, B.Id, p1);
        session3.MarkReady(A.Id);
        session3.MarkReady(C.Id);
        db.Update(session3); await db.SaveChangesAsync();
        publisher.Publish(new BiddingTimerExpired(Guid.NewGuid(), session3.Id, p1.Id));
        await Task.Delay(80);

        // Verifiche finali: rose non vuote e budget coerenti
        Assert.True(A.Budget >= 0 && B.Budget >= 0 && C.Budget >= 0);
        Assert.True(A.CountP + A.CountD + A.CountC + A.CountA > 0);
        Assert.True(B.CountP + B.CountD + B.CountC + B.CountA > 0);
        Assert.True(C.CountP + C.CountD + C.CountC + C.CountA > 0);
    }
}
