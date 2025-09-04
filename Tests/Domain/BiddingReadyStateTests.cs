using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Xunit;

namespace Tests.Domain;

public class BiddingReadyStateTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateReadyState()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var serieAPlayerId = 123;
        var role = PlayerType.Goalkeeper;
        var eligibleTeamIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, serieAPlayerId, role, eligibleTeamIds);

        // Assert
        Assert.Equal(sessionId, readyState.SessionId);
        Assert.Equal(nominatorTeamId, readyState.NominatorTeamId);
        Assert.Equal(serieAPlayerId, readyState.SerieAPlayerId);
        Assert.Equal(role, readyState.Role);
        Assert.Equal(eligibleTeamIds.Count, readyState.EligibleTeamIds.Count);
        Assert.False(readyState.IsCompleted);
        Assert.Empty(readyState.ReadyTeamIds);
        Assert.False(readyState.AllTeamsReady);
    }

    [Fact]
    public void MarkTeamReady_WithEligibleTeam_ShouldAddToReadyList()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 100, PlayerType.Defender, eligibleTeamIds);

        // Act
        var result = readyState.MarkTeamReady(eligibleTeamId);

        // Assert
        Assert.True(result);
        Assert.Contains(eligibleTeamId, readyState.ReadyTeamIds);
        Assert.True(readyState.AllTeamsReady); // Solo un team eligible
    }

    [Fact]
    public void MarkTeamReady_WithNonEligibleTeam_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var nonEligibleTeamId = Guid.NewGuid(); // Team non eligible
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 200, PlayerType.Midfielder, eligibleTeamIds);

        // Act
        var result = readyState.MarkTeamReady(nonEligibleTeamId);

        // Assert
        Assert.False(result);
        Assert.Empty(readyState.ReadyTeamIds);
        Assert.False(readyState.AllTeamsReady);
    }

    [Fact]
    public void MarkTeamReady_WhenAlreadyReady_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 300, PlayerType.Forward, eligibleTeamIds);
        readyState.MarkTeamReady(eligibleTeamId); // Prima volta

        // Act
        var result = readyState.MarkTeamReady(eligibleTeamId); // Seconda volta

        // Assert
        Assert.False(result);
        Assert.Single(readyState.ReadyTeamIds); // Solo una volta nella lista
    }

    [Fact]
    public void MarkTeamReady_WhenCompleted_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 400, PlayerType.Goalkeeper, eligibleTeamIds);
        readyState.MarkTeamReady(eligibleTeamId);
        readyState.Complete(); // Completa manualmente

        // Act
        var result = readyState.MarkTeamReady(eligibleTeamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnmarkTeamReady_WithReadyTeam_ShouldRemoveFromList()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 500, PlayerType.Defender, eligibleTeamIds);
        readyState.MarkTeamReady(eligibleTeamId);

        // Act
        var result = readyState.UnmarkTeamReady(eligibleTeamId);

        // Assert
        Assert.True(result);
        Assert.Empty(readyState.ReadyTeamIds);
        Assert.False(readyState.AllTeamsReady);
    }

    [Fact]
    public void UnmarkTeamReady_WithNonReadyTeam_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 600, PlayerType.Midfielder, eligibleTeamIds);

        // Act
        var result = readyState.UnmarkTeamReady(eligibleTeamId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AllTeamsReady_WithMultipleEligibleTeams_ShouldReturnTrueWhenAllReady()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { team1Id, team2Id, team3Id };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 700, PlayerType.Forward, eligibleTeamIds);

        // Act & Assert - Nessuno pronto
        Assert.False(readyState.AllTeamsReady);

        // Solo team1 pronto
        readyState.MarkTeamReady(team1Id);
        Assert.False(readyState.AllTeamsReady);

        // Team1 e team2 pronti
        readyState.MarkTeamReady(team2Id);
        Assert.False(readyState.AllTeamsReady);

        // Tutti e tre pronti
        readyState.MarkTeamReady(team3Id);
        Assert.True(readyState.AllTeamsReady);
    }

    [Fact]
    public void CompletionPercentage_ShouldCalculateCorrectly()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        var team4Id = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { team1Id, team2Id, team3Id, team4Id }; // 4 team eligible

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 800, PlayerType.Goalkeeper, eligibleTeamIds);

        // Act & Assert
        Assert.Equal(0.0m, readyState.CompletionPercentage); // 0/4 = 0%

        readyState.MarkTeamReady(team1Id);
        Assert.Equal(0.25m, readyState.CompletionPercentage); // 1/4 = 25%

        readyState.MarkTeamReady(team2Id);
        Assert.Equal(0.5m, readyState.CompletionPercentage); // 2/4 = 50%

        readyState.MarkTeamReady(team3Id);
        Assert.Equal(0.75m, readyState.CompletionPercentage); // 3/4 = 75%

        readyState.MarkTeamReady(team4Id);
        Assert.Equal(1.0m, readyState.CompletionPercentage); // 4/4 = 100%
    }

    [Fact]
    public void CompletionPercentage_WithNoEligibleTeams_ShouldReturn100Percent()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid>(); // Nessun team eligible

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 900, PlayerType.Defender, eligibleTeamIds);

        // Act & Assert
        Assert.Equal(1.0m, readyState.CompletionPercentage);
        Assert.True(readyState.AllTeamsReady); // Vacuously true
    }

    [Fact]
    public void GetPendingTeamIds_ShouldReturnUnreadyTeams()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { team1Id, team2Id, team3Id };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 1000, PlayerType.Midfielder, eligibleTeamIds);

        // Act & Assert - Tutti pending
        var pending1 = readyState.GetPendingTeamIds();
        Assert.Equal(3, pending1.Count);
        Assert.Contains(team1Id, pending1);
        Assert.Contains(team2Id, pending1);
        Assert.Contains(team3Id, pending1);

        // Team1 diventa ready
        readyState.MarkTeamReady(team1Id);
        var pending2 = readyState.GetPendingTeamIds();
        Assert.Equal(2, pending2.Count);
        Assert.DoesNotContain(team1Id, pending2);
        Assert.Contains(team2Id, pending2);
        Assert.Contains(team3Id, pending2);

        // Team2 diventa ready
        readyState.MarkTeamReady(team2Id);
        var pending3 = readyState.GetPendingTeamIds();
        Assert.Single(pending3);
        Assert.Contains(team3Id, pending3);

        // Tutti ready
        readyState.MarkTeamReady(team3Id);
        var pending4 = readyState.GetPendingTeamIds();
        Assert.Empty(pending4);
    }

    [Fact]
    public void IsTeamReady_ShouldReturnCorrectStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var nonEligibleTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 1100, PlayerType.Forward, eligibleTeamIds);

        // Act & Assert - Prima di mark ready
        Assert.False(readyState.IsTeamReady(eligibleTeamId));
        Assert.False(readyState.IsTeamReady(nonEligibleTeamId));

        // Dopo mark ready
        readyState.MarkTeamReady(eligibleTeamId);
        Assert.True(readyState.IsTeamReady(eligibleTeamId));
        Assert.False(readyState.IsTeamReady(nonEligibleTeamId));
    }

    [Fact]
    public void Complete_ShouldMarkAsCompleted()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorTeamId = Guid.NewGuid();
        var eligibleTeamId = Guid.NewGuid();
        var eligibleTeamIds = new List<Guid> { eligibleTeamId };

        var readyState = BiddingReadyState.Create(sessionId, nominatorTeamId, 1200, PlayerType.Goalkeeper, eligibleTeamIds);

        // Act
        readyState.Complete();

        // Assert
        Assert.True(readyState.IsCompleted);
        
        // Verifica che non si possa più modificare
        Assert.False(readyState.MarkTeamReady(eligibleTeamId));
        Assert.False(readyState.UnmarkTeamReady(eligibleTeamId));
    }
}
