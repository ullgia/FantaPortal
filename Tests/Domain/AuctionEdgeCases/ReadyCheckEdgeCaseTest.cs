using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Tests.Domain.AuctionEdgeCases;

/// <summary>
/// Edge Case: Gestione del ready check - forza completamento e cambio di stato
/// </summary>
public class ReadyCheckEdgeCaseTest
{
    [Fact]
    public void WhenForceReadyCheckCompletion_ShouldComplete()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Nomina un giocatore per creare BiddingReadyState
        var nominationResult = league.NominatePlayer(teams[0].Id, goalkeeper);
        Assert.True(nominationResult.IsReadyCheck);
        Assert.NotNull(nominationResult.ReadyState);
        
        var auction = league.ActiveAuction;
        Assert.NotNull(auction);
        Assert.NotNull(auction.CurrentReadyState);
        
        // Solo alcuni team pronti (simula scenario parziale)
        var readyState = auction.CurrentReadyState;
        readyState.MarkTeamReady(teams[1].Id);
        // team[2] e team[3] non pronti
        
        Assert.False(readyState.AllTeamsReady);
        
        // Act - Forza il completamento
        readyState.Complete();
        
        // Assert
        Assert.True(readyState.IsCompleted);
    }

    [Fact]
    public void WhenUnconfirmReady_ShouldUpdateState()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Nomina un giocatore per creare BiddingReadyState
        var nominationResult = league.NominatePlayer(teams[0].Id, goalkeeper);
        Assert.True(nominationResult.IsReadyCheck);
        
        var auction = league.ActiveAuction;
        Assert.NotNull(auction);
        var readyState = auction.CurrentReadyState;
        Assert.NotNull(readyState);
        
        // Marca alcuni team come ready
        Assert.True(readyState.MarkTeamReady(teams[1].Id));
        Assert.True(readyState.MarkTeamReady(teams[2].Id));
        
        // Act - Unmark un team
        Assert.True(readyState.UnmarkTeamReady(teams[1].Id));
        
        // Assert - Team1 non dovrebbe essere più ready
        Assert.False(readyState.IsTeamReady(teams[1].Id));
        Assert.True(readyState.IsTeamReady(teams[2].Id));
        Assert.False(readyState.AllTeamsReady);
    }

    [Fact]
    public void WhenReadyCheckCompleted_ShouldPreventFurtherChanges()
    {
        // Arrange
        var league = League.Create("Test League");
        var teams = new List<Team>();

        for (int i = 1; i <= 4; i++)
        {
            teams.Add(league.AddTeam($"Team {i}", 500));
        }
        
        var goalkeeper = SerieAPlayer.Create(1, "P", "P", "GK_Test", "Team_X", 5.5m, 5.0m, 55);
        
        league.StartAuction();
        
        // Nomina un giocatore per creare BiddingReadyState
        var nominationResult = league.NominatePlayer(teams[0].Id, goalkeeper);
        
        var auction = league.ActiveAuction;
        Assert.NotNull(auction);
        var readyState = auction.CurrentReadyState;
        Assert.NotNull(readyState);
        
        readyState.MarkTeamReady(teams[1].Id);
        readyState.MarkTeamReady(teams[2].Id);
        
        // Complete manualmente
        readyState.Complete();
        
        // Act & Assert - Non può più modificare dopo il completamento
        Assert.False(readyState.MarkTeamReady(teams[3].Id)); // Non può più modificare
        Assert.False(readyState.UnmarkTeamReady(teams[2].Id)); // Non può più modificare
        Assert.True(readyState.IsCompleted);
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
