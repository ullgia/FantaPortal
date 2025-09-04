using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Tests.Domain.AuctionEdgeCases;

/// <summary>
/// Edge Case: Scenari estremi per testare robustezza del sistema
/// </summary>
public class ExtremeScenarioEdgeCaseTest
{
    [Fact]
    public void WhenAllTeamsHaveMinimumBudget_ShouldStillWork()
    {
        // Arrange - Tutti i team con budget minimo
        var league = League.Create("Minimum Budget League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            var team = league.AddTeam($"Team {i}", 25); // Budget minimo per 25 giocatori a 1 credito
            teams.Add(team);
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Act - Nomination con budget limitato
        var result = league.NominatePlayer(teams[0].Id, goalkeeper);
        
        // Completa il ready check
        CompleteReadyCheck(league, teams);
        
        // Inizia bidding dopo ready check
        var biddingInfo = league.StartBiddingAfterReady();
        
        // Assert - Tutti dovrebbero essere eligible anche con budget minimo
        Assert.False(result.IsAutoAssign);
        Assert.Equal(4, biddingInfo.EligibleTeams.Count);
        
        // Verifica che tutti possano puntare almeno il minimo
        foreach (var team in teams)
        {
            Assert.Contains(team.Id, biddingInfo.EligibleTeams);
        }
    }

    [Fact]
    public void WhenOnlyOneSlotRemainingPerTeam_ShouldHandleCorrectly()
    {
        // Arrange - Riempi quasi tutti i roster
        var league = League.Create("Nearly Full League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            var team = league.AddTeam($"Team {i}", 500);
            teams.Add(team);
            
            // Riempi tutto tranne 1 portiere
            team.AssignPlayerInternal(PlayerType.Goalkeeper, 1); // 2 portieri
            team.AssignPlayerInternal(PlayerType.Defender, 1);   // 8 difensori  
            team.AssignPlayerInternal(PlayerType.Midfielder, 1); // 8 centrocampisti
            team.AssignPlayerInternal(PlayerType.Forward, 1);    // 6 attaccanti
            // Ora ogni team ha solo 1 slot portiere disponibile
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Act - Nomination quando tutti hanno solo 1 slot
        var result = league.NominatePlayer(teams[0].Id, goalkeeper);
        
        // Completa il ready check
        CompleteReadyCheck(league, teams);
        
        // Inizia bidding dopo ready check
        var biddingInfo = league.StartBiddingAfterReady();
        
        // Assert - Tutti dovrebbero essere eligible
        Assert.False(result.IsAutoAssign);
        Assert.Equal(4, biddingInfo.EligibleTeams.Count);
    }

    [Fact]
    public void WhenMaximumBudgetPerTeam_ShouldHandleHighValues()
    {
        // Arrange - Budget molto alto
        var league = League.Create("High Budget League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            var team = league.AddTeam($"Team {i}", 10000); // Budget molto alto
            teams.Add(team);
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Superstar", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction(basePrice: 100, minIncrement: 50); // Valori alti
        
        // Act - Nomination con budget e prezzi alti
        var result = league.NominatePlayer(teams[0].Id, goalkeeper);
        
        // Completa il ready check
        CompleteReadyCheck(league, teams);
        
        // Inizia bidding dopo ready check
        var biddingInfo = league.StartBiddingAfterReady();
        
        // Assert
        Assert.False(result.IsAutoAssign);
        Assert.Equal(4, biddingInfo.EligibleTeams.Count);
        
        // Test bidding con valori alti
        league.PlaceBid(teams[1].Id, 200); // 100 + 100 (doppio incremento)
        league.PlaceBid(teams[2].Id, 300); // 200 + 100
        
        // Verifica che il sistema gestisca valori alti
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
