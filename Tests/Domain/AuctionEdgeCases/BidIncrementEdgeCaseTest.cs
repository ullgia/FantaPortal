using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Xunit;

namespace Tests.Domain.AuctionEdgeCases;

/// <summary>
/// Edge Case: Enforcement dell'incremento minimo nelle puntate
/// </summary>
public class BidIncrementEdgeCaseTest
{
    [Fact]
    public void WhenBidIncrementIsTooLow_ShouldThrowException()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction(basePrice: 5, minIncrement: 3); // Base 5, incremento minimo 3
        
        var nominationResult = league.NominatePlayer(teams[0].Id, goalkeeper);
        Assert.False(nominationResult.IsAutoAssign);
        
        CompleteReadyCheck(league, teams);
        league.StartBiddingAfterReady();
        
        // Prima puntata valida
        league.PlaceBid(teams[1].Id, 7); // Base 5 + incremento 2 = 7
        
        // Act & Assert - Tenta bid con incremento insufficiente
        Assert.Throws<DomainException>(() => league.PlaceBid(teams[2].Id, 8)); // Solo +1, minimo +3
    }

    [Fact]
    public void WhenBidIncrementIsValid_ShouldSucceed()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction(basePrice: 5, minIncrement: 3);
        
        var nominationResult = league.NominatePlayer(teams[0].Id, goalkeeper);
        Assert.False(nominationResult.IsAutoAssign);
        
        CompleteReadyCheck(league, teams);
        league.StartBiddingAfterReady();
        
        // Prima puntata valida
        league.PlaceBid(teams[1].Id, 7); // Base 5 + incremento 2 = 7
        
        // Act - Bid valido con incremento corretto
        league.PlaceBid(teams[2].Id, 10); // +3 ok
        
        // Assert
        var auction = league.ActiveAuction;
        Assert.NotNull(auction);
        Assert.Equal(AuctionStatus.Running, auction.Status);
    }

    [Fact]
    public void WhenCustomMinimumIncrement_ShouldEnforceCorrectly()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        // Avvia l'asta con incremento minimo personalizzato
        league.StartAuction(basePrice: 5, minIncrement: 5); // Incremento minimo di 5
        
        var nominationResult = league.NominatePlayer(teams[0].Id, goalkeeper);
        Assert.False(nominationResult.IsAutoAssign);
        
        CompleteReadyCheck(league, teams);
        league.StartBiddingAfterReady();
        
        // Prima puntata valida
        league.PlaceBid(teams[1].Id, 10); // Base 5 + incremento 5 = 10
        
        // Act & Assert - Incremento troppo basso per il nuovo minimo
        Assert.Throws<DomainException>(() => league.PlaceBid(teams[2].Id, 12)); // Solo +2, serve +5
        
        // Act - Incremento corretto
        league.PlaceBid(teams[2].Id, 15); // +5 ok
        
        // Assert
        var auction = league.ActiveAuction;
        Assert.NotNull(auction);
        Assert.Equal(AuctionStatus.Running, auction.Status);
    }

    private void CompleteReadyCheck(League league, List<Team> teams)
    {
        var auction = league.ActiveAuction!;
        var readyState = auction.CurrentReadyState!;
        
        // Prendi gli eligible team IDs prima di completare (evita nullref)
        var eligibleTeamIds = readyState.EligibleTeamIds.ToList();
        
        // Conferma tutti i team eligibili per il ready check
        foreach (var eligibleTeamId in eligibleTeamIds)
        {
            league.ConfirmTeamReady(eligibleTeamId);
        }
    }
}
