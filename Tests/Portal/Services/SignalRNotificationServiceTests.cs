/*
File temporaneamente disabilitato per problemi di compilazione con Portal.Hubs e Portal.Services
Verrà riattivato dopo aver sistemato i namespace del progetto Portal

I namespace Portal.Data, Portal.Hubs, Portal.Services non esistono attualmente
Devono essere sostituiti con Infrastructure.Peristance, Infrastructure.Services, etc.
*/
using Application.Events;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Portal.Hubs;
using Portal.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Portal.Services;

public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<AuctionHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IHubClients> _mockClients;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<AuctionHub>>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockClients = new Mock<IHubClients>();
        
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        
        _service = new SignalRNotificationService(_mockHubContext.Object);
    }

    [Fact]
    public async Task NotifyTurnOrderChanged_Should_SendCorrectEvent()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var turnOrder = new List<TurnOrderDto>
        {
            new TurnOrderDto(
                Position: 1,
                TeamId: Guid.NewGuid(),
                TeamName: "Team 1",
                IsCurrentTurn: true)
        };

        // Act
        await _service.NotifyTurnOrderChanged(leagueId, turnOrder);

        // Assert
        _mockClients.Verify(
            c => c.Group(AuctionHub.LeagueGroup(leagueId)),
            Times.Once);
            
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                SignalREventNames.TurnOrderUpdate,
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyPlayerNominated_Should_SendCorrectEvent()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var playerNominated = new PlayerNominatedDto(
            PlayerId: 1,
            PlayerName: "Test Player", 
            Role: PlayerType.Midfielder,
            Team: "Juventus",
            FVM: 25,
            NominatingTeamId: Guid.NewGuid(),
            NominatingTeamName: "My Team");

        // Act
        await _service.NotifyPlayerNominated(leagueId, playerNominated);

        // Assert
        _mockClients.Verify(
            c => c.Group(AuctionHub.LeagueGroup(leagueId)),
            Times.Once);
            
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                SignalREventNames.PlayerNominated,
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyBidPlaced_Should_SendCorrectEvent()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var bid = new BidDto(
            TeamId: Guid.NewGuid(),
            TeamName: "Bidding Team",
            Amount: 50,
            PlacedAt: DateTime.UtcNow);

        // Act
        await _service.NotifyBidPlaced(leagueId, bid);

        // Assert
        _mockClients.Verify(
            c => c.Group(AuctionHub.LeagueGroup(leagueId)),
            Times.Once);
            
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                SignalREventNames.BidUpdate,
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyReadyStatesChanged_Should_SendCorrectEvent()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var readyStates = new List<ReadyStateDto>
        {
            new ReadyStateDto(TeamId: Guid.NewGuid(), TeamName: "Team 1", IsReady: true),
            new ReadyStateDto(TeamId: Guid.NewGuid(), TeamName: "Team 2", IsReady: false)
        };

        // Act
        await _service.NotifyReadyStatesChanged(leagueId, readyStates);

        // Assert
        _mockClients.Verify(
            c => c.Group(AuctionHub.LeagueGroup(leagueId)),
            Times.Once);
            
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                SignalREventNames.ReadyStatesUpdate,
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyFullStateUpdate_Should_SendCorrectEvent()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var turnOrder = new List<TurnOrderDto>
        {
            new TurnOrderDto(1, Guid.NewGuid(), "Team 1", true)
        };
        var overview = new AuctionOverviewDto(
            AuctionId: leagueId,
            LeagueName: "Test League",
            Status: AuctionStatus.Running,
            CurrentRole: PlayerType.Forward,
            CurrentTurnPosition: 2,
            TotalTeams: 4,
            CurrentTurnTeamId: Guid.NewGuid(),
            CurrentTurnTeamName: "Current Team",
            TurnOrder: turnOrder,
            IsBiddingActive: true,
            IsReadyCheckActive: false);

        // Act
        await _service.NotifyFullStateUpdate(leagueId, overview);

        // Assert
        _mockClients.Verify(
            c => c.Group(AuctionHub.LeagueGroup(leagueId)),
            Times.Once);
            
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                SignalREventNames.FullStateUpdate,
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);
    }

    [Fact]
    public async Task NotifyPhaseChanged_Should_SendCorrectEvent()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var newPhase = "Bidding";

        // Act
        await _service.NotifyPhaseChanged(leagueId, newPhase);

        // Assert
        _mockClients.Verify(
            c => c.Group(AuctionHub.LeagueGroup(leagueId)),
            Times.Once);
            
        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                SignalREventNames.PhaseChanged,
                It.Is<object[]>(args => args.Length == 1),
                default),
            Times.Once);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("12345678-1234-5678-9012-123456789012")]
    public void AuctionHub_LeagueGroup_Should_GenerateCorrectGroupName(string guidString)
    {
        // Arrange
        var leagueId = Guid.Parse(guidString);
        var expectedGroupName = $"league:{leagueId}";

        // Act
        var actualGroupName = AuctionHub.LeagueGroup(leagueId);

        // Assert
        Assert.Equal(expectedGroupName, actualGroupName);
    }
}
