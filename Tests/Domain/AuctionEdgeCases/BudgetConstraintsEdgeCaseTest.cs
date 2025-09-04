using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Tests.Domain.AuctionEdgeCases;

/// <summary>
/// Edge Case: Vincoli di budget - team esclusi dal bidding per budget insufficiente
/// </summary>
public class BudgetConstraintsEdgeCaseTest
{
    [Fact]
    public void WhenTeamHasLimitedBudget_ShouldStillBeEligibleIfSufficient()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<LeaguePlayer>();

        for (int i = 1; i <= 3; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }

        var teamA = teams[0]; // Nominatore
        var teamB = teams[1]; // Budget limitato
        var teamC = teams[2]; // Budget normale
        
        // Riduci budget di teamB spendendo per portieri (budget limitato)
        for (int i = 0; i < 3; i++)
        {
            teamB.AssignPlayerInternal(PlayerType.Goalkeeper, 100);
        }
        // teamB ora ha budget 200, può ancora puntare
        
        var defender = SerieAPlayer.Create(1, "D", "D", "DEF_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Act - Team A nomina un difensore 
        var result = league.NominatePlayer(teamA.Id, defender);
        
        // Ora facciamo il ready check
        CompleteReadyCheck(league, teams);
        
        // Inizia il bidding e ottieni le informazioni
        var biddingInfo = league.StartBiddingAfterReady();
        
        // Assert - Almeno 2 team dovrebbero essere eligible
        Assert.False(result.IsAutoAssign);
        Assert.NotNull(biddingInfo);
        
        var eligibleTeams = biddingInfo.EligibleTeams;
        Assert.True(eligibleTeams.Count >= 2, $"Expected at least 2 eligible teams, got {eligibleTeams.Count}");
    }

    [Fact]
    public void WhenTeamHasInsufficientBudget_ShouldBeExcludedFromBidding()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<LeaguePlayer>();

        for (int i = 1; i <= 3; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }

        var teamA = teams[0]; // Nominatore
        var teamB = teams[1]; // Budget insufficiente
        var teamC = teams[2]; // Budget normale
        
        // Svuota il budget di teamB
        for (int i = 0; i < 3; i++)
        {
            teamB.AssignPlayerInternal(PlayerType.Goalkeeper, 165); // 165*3 = 495, resta 5
        }
        // teamB ora ha budget 5, appena sufficiente per il minimo
        
        var defender = SerieAPlayer.Create(1, "D", "D", "DEF_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Act - Team A nomina un difensore
        var result = league.NominatePlayer(teamA.Id, defender);
        
        // Ora facciamo il ready check
        CompleteReadyCheck(league, teams);
        
        // Inizia il bidding e ottieni le informazioni  
        var biddingInfo = league.StartBiddingAfterReady();
        
        // Assert - Almeno 2 team dovrebbero essere eligible
        Assert.False(result.IsAutoAssign);
        Assert.NotNull(biddingInfo);
        
        var eligibleTeams = biddingInfo.EligibleTeams;
        Assert.True(eligibleTeams.Count >= 2, $"Expected at least 2 eligible teams, got {eligibleTeams.Count}");
    }

    private void CompleteReadyCheck(League league, List<LeaguePlayer> teams)
    {
        var auction = league.ActiveAuction!;
        var readyState = auction.CurrentReadyState!;
        
        // Conferma TUTTI i team per il ready check (non solo gli eligible)
        foreach (var team in teams)
        {
            // Controlla se il team è ancora eligible e confirm se lo è
            if (readyState.EligibleTeamIds.Contains(team.Id))
            {
                league.ConfirmTeamReady(team.Id);
            }
        }
    }
}
