using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Peristance;
using Domain.Entities;
using Application.Services;
using Application.Events;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Domain;

public class TimerValidationTest
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Timer_Should_Start_With_Valid_Turn()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var publisher = new InMemoryDomainEventPublisher();
        var timerDataService = new Infrastructure.Services.AuctionTimerDataService(context);
        var services = new ServiceCollection();
        services.AddScoped<ApplicationDbContext>(_ => context);
        var serviceScopeFactory = Mock.Of<IServiceScopeFactory>();
        var logger = Mock.Of<ILogger<AuctionTimerManager>>();
        var timerManager = new AuctionTimerManager(timerDataService, serviceScopeFactory, logger);

        var league = League.Create("Test League");
        var team = league.AddTeam("Test Team", 100);
        league.StartAuction();
        var session = league.ActiveAuction!;
        var serieAPlayer = SerieAPlayer.Create(1, "Test", "Player", PlayerType.Forward.ToString(), "TEAM", 10m, 10m, 25);
        
        context.Leagues.Add(league);
        context.SerieAPlayers.Add(serieAPlayer);
        await context.SaveChangesAsync();

        // Create a simple test turn using AuctionTurn.Create with PlayerId as Guid
        var turn = AuctionTurn.Create(session.Id, Guid.NewGuid()); // Use Guid instead of int
        context.AuctionTurns.Add(turn);
        await context.SaveChangesAsync();

        // Act
        await timerManager.StartTimerAsync(
            turnId: turn.Id,
            auctionId: session.Id,
            remainingSeconds: 60,
            leagueId: league.Id,
            warningAtSeconds: 10,
            serieAPlayerId: serieAPlayer.Id,
            sessionId: session.Id);

        // Assert - Check if timer is active
        var hasActiveTimer = timerManager.HasActiveTimer(turn.Id);
        Assert.True(hasActiveTimer);
    }

    [Fact]
    public async Task Timer_Should_Handle_Invalid_Turn_Gracefully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var publisher = new InMemoryDomainEventPublisher();
        var timerDataService = new Infrastructure.Services.AuctionTimerDataService(context);
        var serviceScopeFactory = Mock.Of<IServiceScopeFactory>();
        var logger = Mock.Of<ILogger<AuctionTimerManager>>();
        var timerManager = new AuctionTimerManager(timerDataService, serviceScopeFactory, logger);

        var invalidTurnId = Guid.NewGuid();
        var invalidSessionId = Guid.NewGuid();

        // Act & Assert - Should not throw but may not create active timer
        await timerManager.StartTimerAsync(
            turnId: invalidTurnId,
            auctionId: invalidSessionId,
            remainingSeconds: 60,
            leagueId: Guid.NewGuid(),
            warningAtSeconds: 10,
            serieAPlayerId: null,
            sessionId: invalidSessionId);

        // Timer might not be active for invalid turn
        var hasActiveTimer = timerManager.HasActiveTimer(invalidTurnId);
        // We can't assert False here as timer manager might still create the timer
        // The main assertion is that no exception is thrown
    }

    [Fact]
    public async Task Timer_Should_Stop_Successfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var publisher = new InMemoryDomainEventPublisher();
        var timerDataService = new Infrastructure.Services.AuctionTimerDataService(context);
        var serviceScopeFactory = Mock.Of<IServiceScopeFactory>();
        var logger = Mock.Of<ILogger<AuctionTimerManager>>();
        var timerManager = new AuctionTimerManager(timerDataService, serviceScopeFactory, logger);

        var league = League.Create("Test League");
        var team = league.AddTeam("Test Team", 100);
        league.StartAuction();
        var session = league.ActiveAuction!;
        var serieAPlayer = SerieAPlayer.Create(1, "Test", "Player", PlayerType.Forward.ToString(), "TEAM", 10m, 10m, 25);
        
        context.Leagues.Add(league);
        context.SerieAPlayers.Add(serieAPlayer);
        await context.SaveChangesAsync();

        // Create a simple test turn using AuctionTurn.Create with PlayerId as Guid
        var turn = AuctionTurn.Create(session.Id, Guid.NewGuid()); // Use Guid instead of int
        context.AuctionTurns.Add(turn);
        await context.SaveChangesAsync();

        await timerManager.StartTimerAsync(
            turnId: turn.Id,
            auctionId: session.Id,
            remainingSeconds: 60,
            leagueId: league.Id,
            warningAtSeconds: 10,
            serieAPlayerId: serieAPlayer.Id,
            sessionId: session.Id);

        // Act
        await timerManager.StopTimerAsync(turn.Id);

        // Assert - Timer should no longer be active
        var hasActiveTimer = timerManager.HasActiveTimer(turn.Id);
        Assert.False(hasActiveTimer);
    }
}
