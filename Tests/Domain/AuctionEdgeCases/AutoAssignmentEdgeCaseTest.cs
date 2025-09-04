using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Tests.Domain.AuctionEdgeCases;

/// <summary>
/// Edge Case: Auto-assegnazione quando solo il nominatore ha slots disponibili
/// </summary>
public class AutoAssignmentEdgeCaseTest
{
    [Fact]
    public void WhenOnlyNominatorHasSlots_ShouldAutoAssign()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }

        // Riempi i portieri di tutti i team tranne il nominatore
        for (int i = 1; i < teams.Count; i++)
        {
            FillGoalkeepers(teams[i], 3); // Riempi completamente
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Act - Team[0] nomina un portiere (solo lui ha slot)
        var result = league.NominatePlayer(teams[0].Id, goalkeeper);
        
        // Assert - Dovrebbe essere auto-assegnato
        Assert.True(result.IsAutoAssign);
        Assert.Equal(1, result.Price); // Prezzo base default
        Assert.Equal(PlayerType.Goalkeeper, result.Role);
        
        // Verifica assegnazione
        Assert.Equal(2, teams[0].GetAvailableSlots(PlayerType.Goalkeeper)); // 3-1=2
        Assert.Equal(499, teams[0].Budget); // 500-1=499 (auto-assign price)
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

    private void FillGoalkeepers(Team team, int count)
    {
        for (int i = 0; i < count; i++)
        {
            team.AssignPlayerInternal(PlayerType.Goalkeeper, 1);
        }
    }
}
