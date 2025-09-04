using Application.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests.Application.Services;

public class DtoMappingTests
{
    [Fact]
    public void TurnOrderDto_Should_MapCorrectly_FromBasicData()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamName = "Team Test";
        var isCurrentTurn = true;
        var position = 1;

        // Act
        var dto = new TurnOrderDto(
            Position: position,
            TeamId: teamId,
            TeamName: teamName,
            IsCurrentTurn: isCurrentTurn
        );

        // Assert
        Assert.Equal(1, dto.Position);
        Assert.Equal(teamId, dto.TeamId);
        Assert.Equal("Team Test", dto.TeamName);
        Assert.True(dto.IsCurrentTurn);
    }

    [Fact]
    public void PlayerNominatedDto_Should_MapCorrectly_FromBasicData()
    {
        // Arrange
        var playerId = 1;
        var playerName = "Mario Rossi";
        var team = "Juventus";
        var role = PlayerType.Goalkeeper;
        var fvm = 25m;
        var nominatingTeamId = Guid.NewGuid();
        var nominatingTeamName = "La Mia Squadra";

        // Act
        var dto = new PlayerNominatedDto(
            PlayerId: playerId,
            PlayerName: playerName,
            Role: role,
            Team: team,
            FVM: fvm,
            NominatingTeamId: nominatingTeamId,
            NominatingTeamName: nominatingTeamName
        );

        // Assert
        Assert.Equal(1, dto.PlayerId);
        Assert.Equal("Mario Rossi", dto.PlayerName);
        Assert.Equal("Juventus", dto.Team);
        Assert.Equal(PlayerType.Goalkeeper, dto.Role);
        Assert.Equal("La Mia Squadra", dto.NominatingTeamName);
        Assert.Equal(25m, dto.FVM);
    }

    [Fact]
    public void BidDto_Should_MapCorrectly_FromBasicData()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamName = "Squadra Test";
        var amount = 50;
        var placedAt = DateTime.UtcNow;

        // Act
        var dto = new BidDto(
            TeamId: teamId,
            TeamName: teamName,
            Amount: amount,
            PlacedAt: placedAt
        );

        // Assert
        Assert.Equal(50, dto.Amount);
        Assert.Equal(teamId, dto.TeamId);
        Assert.Equal("Squadra Test", dto.TeamName);
        Assert.Equal(placedAt, dto.PlacedAt);
    }

    [Fact]
    public void ReadyStateDto_Should_MapCorrectly()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamName = "Test Team";
        var isReady = true;

        // Act
        var dto = new ReadyStateDto(
            TeamId: teamId,
            TeamName: teamName,
            IsReady: isReady
        );

        // Assert
        Assert.Equal(teamId, dto.TeamId);
        Assert.Equal("Test Team", dto.TeamName);
        Assert.True(dto.IsReady);
    }

    [Fact]
    public void AuctionOverviewDto_Should_MapCorrectly()
    {
        // Arrange
        var auctionId = Guid.NewGuid();
        var leagueName = "Serie A Test";
        var status = AuctionStatus.Running;
        var currentRole = PlayerType.Midfielder;
        var currentTurnPosition = 2;
        var totalTeams = 4;
        var currentTurnTeamId = Guid.NewGuid();
        var currentTurnTeamName = "Current Team";
        var turnOrder = new List<TurnOrderDto>();
        var isBiddingActive = true;
        var isReadyCheckActive = false;

        // Act
        var dto = new AuctionOverviewDto(
            AuctionId: auctionId,
            LeagueName: leagueName,
            Status: status,
            CurrentRole: currentRole,
            CurrentTurnPosition: currentTurnPosition,
            TotalTeams: totalTeams,
            CurrentTurnTeamId: currentTurnTeamId,
            CurrentTurnTeamName: currentTurnTeamName,
            TurnOrder: turnOrder,
            IsBiddingActive: isBiddingActive,
            IsReadyCheckActive: isReadyCheckActive
        );

        // Assert
        Assert.Equal(auctionId, dto.AuctionId);
        Assert.Equal("Serie A Test", dto.LeagueName);
        Assert.Equal(AuctionStatus.Running, dto.Status);
        Assert.Equal(2, dto.CurrentTurnPosition);
        Assert.Equal(4, dto.TotalTeams);
        Assert.Equal(PlayerType.Midfielder, dto.CurrentRole);
        Assert.Equal(currentTurnTeamId, dto.CurrentTurnTeamId);
        Assert.False(dto.IsReadyCheckActive);
        Assert.True(dto.IsBiddingActive);
    }

    [Theory]
    [InlineData(PlayerType.Goalkeeper)]
    [InlineData(PlayerType.Defender)]
    [InlineData(PlayerType.Midfielder)]
    [InlineData(PlayerType.Forward)]
    public void PlayerNominatedDto_Should_HandleAllRoleTypes(PlayerType role)
    {
        // Arrange & Act
        var dto = new PlayerNominatedDto(
            PlayerId: 1,
            PlayerName: "Test Player",
            Role: role,
            Team: "Test Team",
            FVM: 20m,
            NominatingTeamId: Guid.NewGuid(),
            NominatingTeamName: "Nominating Team"
        );

        // Assert
        Assert.Equal(role, dto.Role);
        Assert.True(Enum.IsDefined(typeof(PlayerType), dto.Role));
    }

    [Theory]
    [InlineData(0, "Nessuna offerta")]
    [InlineData(1, "Un milione")]
    [InlineData(100, "Cento milioni")]
    [InlineData(999, "Record assoluto")]
    public void BidDto_Should_HandleVariousAmounts(int amount, string description)
    {
        // Arrange & Act
        var dto = new BidDto(
            TeamId: Guid.NewGuid(),
            TeamName: description,
            Amount: amount,
            PlacedAt: DateTime.UtcNow
        );

        // Assert
        Assert.Equal(amount, dto.Amount);
        Assert.True(dto.Amount >= 0);
    }
}
