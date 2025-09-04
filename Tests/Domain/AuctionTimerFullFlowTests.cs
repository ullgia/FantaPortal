using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Events;
using Application.Events.Handlers;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Infrastructure.Peristance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Domain;

/// <summary>
/// Test completo del timer di asta con un esempio realistico di funzionamento
/// Simula uno scenario end-to-end dalla nomination al timeout automatico
/// </summary>
public class AuctionTimerFullFlowTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ServiceProvider _serviceProvider;
    private Mock<IRealtimeNotificationService> _signalRMock;

    public AuctionTimerFullFlowTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup services con minimo mocking
        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddSingleton<IAuctionTimerDataService, AuctionTimerDataService>();
        services.AddSingleton<Mock<IRealtimeNotificationService>>(provider => {
            _signalRMock = new Mock<IRealtimeNotificationService>();
            return _signalRMock;
        });
        services.AddSingleton<IRealtimeNotificationService>(provider => 
            provider.GetRequiredService<Mock<IRealtimeNotificationService>>().Object);
        
        // Logger mocks
        services.AddSingleton<ILogger<AuctionTimerManager>>(provider => 
            Mock.Of<ILogger<AuctionTimerManager>>());
        services.AddSingleton<ILogger<BiddingTimerExpiredHandler>>(provider => 
            Mock.Of<ILogger<BiddingTimerExpiredHandler>>());
        
        // Real services
        services.AddSingleton<IAuctionTimerManager, AuctionTimerManager>();
        services.AddSingleton<AuctionTimerService>();
        services.AddTransient<IAuctionCommands, AuctionCommands>();
        services.AddTransient<IDomainEventPublisher, InMemoryDomainEventPublisher>();
        services.AddTransient<BiddingTimerExpiredHandler>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task AuctionTimer_CompleteFlow_ShouldWorkEndToEnd()
    {
        // Arrange - Setup completo con asta reale
        var league = await SetupCompleteAuctionScenario();
        var timerManager = _serviceProvider.GetRequiredService<IAuctionTimerManager>();
        var timerService = _serviceProvider.GetRequiredService<AuctionTimerService>();
        var auctionCommands = _serviceProvider.GetRequiredService<IAuctionCommands>();

        var currentAuction = league.ActiveAuction!;
        var turnId = Guid.NewGuid();
        var currentPlayerId = currentAuction.CurrentSerieAPlayerId;

        // Verifica stato iniziale
        Assert.True(currentAuction.IsBiddingActive);
        Assert.Equal(AuctionStatus.Running, currentAuction.Status);

        // Act 1 - Start timer per bidding
        await timerService.StartBiddingTimerAsync(
            turnId: turnId,
            auctionId: currentAuction.Id,
            sessionId: currentAuction.Id, 
            leagueId: league.Id,
            serieAPlayerId: currentPlayerId,
            durationSeconds: 2, // Timer breve per test
            warningAtSeconds: 1);

        // Verifica che il timer sia attivo
        Assert.True(timerManager.HasActiveTimer(turnId));

        // Act 2 - Attendi scadenza timer (simula scenario reale)
        await Task.Delay(3000); // Attendi che il timer scada

        // Attendi che l'handler abbia processato l'evento
        await Task.Delay(500);

        // Assert - Verifica che il bidding sia stato finalizzato automaticamente
        _context.Entry(league).Reload();
        await _context.Entry(league).Reference(l => l.ActiveAuction).LoadAsync();
        await _context.Entry(league).Collection(l => l.PlayerOwnerships).LoadAsync();
        await _context.Entry(league).Collection(l => l.Teams).LoadAsync();

        // Il bidding dovrebbe essere completato
        Assert.False(league.ActiveAuction.IsBiddingActive);
        
        // Il giocatore dovrebbe essere stato assegnato
        Assert.True(league.PlayerOwnerships.Count > 0);
        var assignment = league.PlayerOwnerships.FirstOrDefault(po => po.SerieAPlayerId == currentPlayerId);
        Assert.NotNull(assignment);

        // Il timer non dovrebbe più essere attivo
        Assert.False(timerManager.HasActiveTimer(turnId));

        // Verifica che SignalR sia stato chiamato per updates
        _signalRMock.Verify(s => s.TimerUpdate(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task AuctionTimer_WhenStoppedManually_ShouldNotExpire()
    {
        // Arrange
        var league = await SetupCompleteAuctionScenario();
        var timerManager = _serviceProvider.GetRequiredService<IAuctionTimerManager>();
        var timerService = _serviceProvider.GetRequiredService<AuctionTimerService>();

        var currentAuction = league.ActiveAuction!;
        var turnId = Guid.NewGuid();
        var currentPlayerId = currentAuction.CurrentSerieAPlayerId;

        // Start timer
        await timerService.StartBiddingTimerAsync(
            turnId: turnId,
            auctionId: currentAuction.Id,
            sessionId: currentAuction.Id,
            leagueId: league.Id,
            serieAPlayerId: currentPlayerId,
            durationSeconds: 5,
            warningAtSeconds: 1);

        Assert.True(timerManager.HasActiveTimer(turnId));

        // Act - Stop timer manualmente prima della scadenza
        await timerService.StopBiddingTimerAsync(turnId);

        // Attendi per assicurarsi che non scada
        await Task.Delay(6000);

        // Assert - Il bidding dovrebbe ancora essere attivo
        _context.Entry(league).Reload();
        await _context.Entry(league).Reference(l => l.ActiveAuction).LoadAsync();

        Assert.True(league.ActiveAuction.IsBiddingActive);
        Assert.False(timerManager.HasActiveTimer(turnId));
    }

    [Fact]
    public async Task AuctionTimer_WhenPausedAndResumed_ShouldWorkCorrectly()
    {
        // Arrange
        var league = await SetupCompleteAuctionScenario();
        var timerManager = _serviceProvider.GetRequiredService<IAuctionTimerManager>();
        var timerService = _serviceProvider.GetRequiredService<AuctionTimerService>();

        var currentAuction = league.ActiveAuction!;
        var turnId = Guid.NewGuid();
        var currentPlayerId = currentAuction.CurrentSerieAPlayerId;

        // Start timer
        await timerService.StartBiddingTimerAsync(
            turnId: turnId,
            auctionId: currentAuction.Id,
            sessionId: currentAuction.Id,
            leagueId: league.Id,
            serieAPlayerId: currentPlayerId,
            durationSeconds: 4,
            warningAtSeconds: 1);

        // Act - Pause, wait, resume
        await Task.Delay(1000); // Lascia che il timer giri per 1 secondo
        await timerService.PauseBiddingTimerAsync(turnId);
        
        await Task.Delay(2000); // Attendi 2 secondi in pausa
        
        await timerService.ResumeBiddingTimerAsync(turnId);
        await Task.Delay(4000); // Attendi scadenza

        // Assert - Il timer dovrebbe scadere dopo la ripresa
        _context.Entry(league).Reload();
        await _context.Entry(league).Reference(l => l.ActiveAuction).LoadAsync();
        await _context.Entry(league).Collection(l => l.PlayerOwnerships).LoadAsync();

        // Il bidding dovrebbe essere finalizzato
        Assert.False(league.ActiveAuction.IsBiddingActive);
        Assert.True(league.PlayerOwnerships.Count > 0);
    }

    [Fact]
    public async Task AuctionTimerDataService_ShouldPersistAndRetrieveTimers()
    {
        // Arrange
        var dataService = _serviceProvider.GetRequiredService<IAuctionTimerDataService>();
        var timer1 = PersistedTimer.Create(
            turnId: Guid.NewGuid(),
            auctionId: Guid.NewGuid(),
            initialSeconds: 60,
            warningAtSeconds: 10,
            leagueId: Guid.NewGuid(),
            serieAPlayerId: 123,
            sessionId: Guid.NewGuid());
        
        var timer2 = PersistedTimer.Create(
            turnId: Guid.NewGuid(),
            auctionId: timer1.AuctionId, // Stessa auction
            initialSeconds: 30,
            warningAtSeconds: 5,
            leagueId: timer1.LeagueId,
            serieAPlayerId: 456,
            sessionId: Guid.NewGuid());

        // Act - Save timers
        await dataService.SaveTimerAsync(timer1);
        await dataService.SaveTimerAsync(timer2);

        // Test retrieval
        var retrieved1 = await dataService.GetTimerAsync(timer1.TurnId);
        var retrieved2 = await dataService.GetTimerAsync(timer2.TurnId);
        var activeTimers = await dataService.GetActiveTimersAsync();
        var auctionTimers = await dataService.GetTimersForAuctionAsync(timer1.AuctionId);

        // Assert
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal(timer1.TurnId, retrieved1.TurnId);
        Assert.Equal(timer2.TurnId, retrieved2.TurnId);
        
        Assert.Contains(activeTimers, t => t.TurnId == timer1.TurnId);
        Assert.Contains(activeTimers, t => t.TurnId == timer2.TurnId);
        
        Assert.Equal(2, auctionTimers.Count);
        Assert.All(auctionTimers, t => Assert.Equal(timer1.AuctionId, t.AuctionId));

        // Test deletion
        await dataService.DeleteTimerAsync(timer1.TurnId);
        var deletedTimer = await dataService.GetTimerAsync(timer1.TurnId);
        Assert.Null(deletedTimer);
    }

    private async Task<League> SetupCompleteAuctionScenario()
    {
        // Crea league con 4 teams
        var league = League.Create("Timer Test League");
        var team1 = league.AddTeam("Team A", 500);
        var team2 = league.AddTeam("Team B", 500);
        var team3 = league.AddTeam("Team C", 500);
        var team4 = league.AddTeam("Team D", 500);

        // Crea un giocatore per il test
        var goalkeeper = SerieAPlayer.Create(200, "P", "P", "Timer_GK", "Team_X", 5.5m, 5.0m, 55);
        _context.SerieAPlayers.Add(goalkeeper);

        // Salva nel database
        _context.Leagues.Add(league);
        await _context.SaveChangesAsync();

        // Avvia asta e setup bidding scenario completo
        league.StartAuction(basePrice: 1, minIncrement: 1);
        
        // Nomina giocatore
        var nominationResult = league.NominatePlayer(team1.Id, goalkeeper);
        Assert.False(nominationResult.IsAutoAssign);
        
        // Completa ready check
        var readyState = league.ActiveAuction!.CurrentReadyState!;
        foreach (var eligibleTeamId in readyState.EligibleTeamIds)
        {
            league.ConfirmTeamReady(eligibleTeamId);
        }
        
        // Avvia bidding
        var biddingInfo = league.StartBiddingAfterReady();
        Assert.NotNull(biddingInfo);
        Assert.True(league.ActiveAuction.IsBiddingActive);
        
        // Piazza alcune offerte per rendere realistico
        if (biddingInfo.EligibleTeams.Count > 1)
        {
            var eligibleTeams = biddingInfo.EligibleTeams.ToList();
            league.PlaceBid(eligibleTeams[0], 2); // Prima offerta
            if (eligibleTeams.Count > 1)
            {
                league.PlaceBid(eligibleTeams[1], 3); // Seconda offerta (vincente se timer scade)
            }
        }

        // Salva stato finale
        _context.Update(league);
        await _context.SaveChangesAsync();

        return league;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        _context?.Dispose();
    }
}
