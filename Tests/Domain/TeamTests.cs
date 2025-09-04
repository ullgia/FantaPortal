using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Xunit;

namespace Tests.Domain;

public class TeamTests
{
    [Fact]
    public void CreateInternal_WithValidData_ShouldCreateTeam()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var teamName = "Test Team";
        var initialBudget = 500;

        // Act
        var team = Team.CreateInternal(leagueId, teamName, initialBudget);

        // Assert
        Assert.Equal(leagueId, team.LeagueId);
        Assert.Equal(teamName, team.Name);
        Assert.Equal(initialBudget, team.Budget);
        Assert.Equal(0, team.CountP);
        Assert.Equal(0, team.CountD);
        Assert.Equal(0, team.CountC);
        Assert.Equal(0, team.CountA);
    }

    [Theory]
    [InlineData(PlayerType.Goalkeeper, true)]   // Può ancora comprare portieri (max 3)
    [InlineData(PlayerType.Defender, true)]   // Può ancora comprare difensori (max 8)
    [InlineData(PlayerType.Midfielder, true)]   // Può ancora comprare centrocampisti (max 8)
    [InlineData(PlayerType.Forward, true)]   // Può ancora comprare attaccanti (max 6)
    public void HasSlot_WithAvailableSlots_ShouldReturnTrue(PlayerType role, bool expected)
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 500);

        // Act
        var hasSlot = team.HasSlot(role);

        // Assert
        Assert.Equal(expected, hasSlot);
    }

    [Fact]
    public void GetAvailableSlots_WithNoAssignments_ShouldReturnMaxSlots()
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 500);

        // Act & Assert
        Assert.Equal(3, team.GetAvailableSlots(PlayerType.Goalkeeper));
        Assert.Equal(8, team.GetAvailableSlots(PlayerType.Defender));
        Assert.Equal(8, team.GetAvailableSlots(PlayerType.Midfielder));
        Assert.Equal(6, team.GetAvailableSlots(PlayerType.Forward));
    }

    [Fact]
    public void AssignPlayerInternal_WithValidData_ShouldUpdateBudgetAndCounts()
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 500);
        var price = 50;

        // Act
        team.AssignPlayerInternal(PlayerType.Goalkeeper, price);

        // Assert
        Assert.Equal(450, team.Budget); // 500 - 50
        Assert.Equal(1, team.CountP);
        Assert.Equal(2, team.GetAvailableSlots(PlayerType.Goalkeeper)); // 3 - 1
    }

    [Fact]
    public void AssignPlayerInternal_WithInsufficientBudget_ShouldThrowException()
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 100);
        var price = 150; // Più del budget disponibile

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => team.AssignPlayerInternal(PlayerType.Goalkeeper, price));
        Assert.Contains("Insufficient budget", exception.Message);
    }

    [Fact]
    public void AssignPlayerInternal_WithNoAvailableSlot_ShouldThrowException()
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 500);
        
        // Riempie tutti i slot per portieri
        team.AssignPlayerInternal(PlayerType.Goalkeeper, 50);
        team.AssignPlayerInternal(PlayerType.Goalkeeper, 50);
        team.AssignPlayerInternal(PlayerType.Goalkeeper, 50);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => team.AssignPlayerInternal(PlayerType.Goalkeeper, 50));
        Assert.Contains("No available slot", exception.Message);
    }

    [Fact]
    public void ReleasePlayerInternal_ShouldRestoreBudgetAndDecrementCount()
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 500);
        team.AssignPlayerInternal(PlayerType.Defender, 75);

        // Verifica stato iniziale
        Assert.Equal(425, team.Budget);
        Assert.Equal(1, team.CountD);

        // Act
        team.ReleasePlayerInternal(PlayerType.Defender, 75);

        // Assert
        Assert.Equal(500, team.Budget); // Budget ripristinato
        Assert.Equal(0, team.CountD);   // Contatore decrementato
        Assert.Equal(8, team.GetAvailableSlots(PlayerType.Defender)); // Slot disponibile
    }

    [Fact]
    public void GetPlayerCounts_ShouldReturnCorrectCounts()
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 500);
        team.AssignPlayerInternal(PlayerType.Goalkeeper, 50);
        team.AssignPlayerInternal(PlayerType.Defender, 30);
        team.AssignPlayerInternal(PlayerType.Defender, 40);
        team.AssignPlayerInternal(PlayerType.Midfielder, 60);
        team.AssignPlayerInternal(PlayerType.Forward, 80);

        // Act
        var counts = team.GetPlayerCounts();

        // Assert
        Assert.Equal(1, counts.P);
        Assert.Equal(2, counts.D);
        Assert.Equal(1, counts.C);
        Assert.Equal(1, counts.A);
    }

    [Fact]
    public void MultipleOperations_ShouldMaintainConsistentState()
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 1000);

        // Act - Sequence di operazioni
        team.AssignPlayerInternal(PlayerType.Goalkeeper, 100);  // Budget: 900, P: 1
        team.AssignPlayerInternal(PlayerType.Defender, 150);  // Budget: 750, D: 1
        team.ReleasePlayerInternal(PlayerType.Goalkeeper, 100); // Budget: 850, P: 0
        team.AssignPlayerInternal(PlayerType.Forward, 200);  // Budget: 650, A: 1

        // Assert
        Assert.Equal(650, team.Budget);
        Assert.Equal(0, team.CountP);
        Assert.Equal(1, team.CountD);
        Assert.Equal(0, team.CountC);
        Assert.Equal(1, team.CountA);

        Assert.Equal(3, team.GetAvailableSlots(PlayerType.Goalkeeper)); // Tutti disponibili
        Assert.Equal(7, team.GetAvailableSlots(PlayerType.Defender)); // 8 - 1
        Assert.Equal(8, team.GetAvailableSlots(PlayerType.Midfielder)); // Tutti disponibili
        Assert.Equal(5, team.GetAvailableSlots(PlayerType.Forward)); // 6 - 1
    }

    [Theory]
    [InlineData(PlayerType.Goalkeeper, 3)]
    [InlineData(PlayerType.Defender, 8)]
    [InlineData(PlayerType.Midfielder, 8)]
    [InlineData(PlayerType.Forward, 6)]
    public void FillAllSlots_ShouldReachMaxCapacity(PlayerType role, int maxSlots)
    {
        // Arrange
        var team = Team.CreateInternal(Guid.NewGuid(), "Test Team", 5000); // Budget alto per non limitare
        
        // Act - Riempie tutti gli slot per il ruolo
        for (int i = 0; i < maxSlots; i++)
        {
            team.AssignPlayerInternal(role, 10);
        }

        // Assert
        Assert.Equal(0, team.GetAvailableSlots(role));
        Assert.False(team.HasSlot(role));
        
        // Verifica che non può aggiungere altri
        Assert.Throws<DomainException>(() => team.AssignPlayerInternal(role, 10));
    }
}
