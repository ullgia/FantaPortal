using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Xunit;

namespace Tests.Domain;

public class SimpleAutoAssignmentTest
{
    [Fact]
    public void EvaluateNomination_SimpleTest_WithFixedIds()
    {
        // Arrange - create teams with automatic IDs
        var leagueId = Guid.NewGuid();
        
        // Crea i team con IDs automatici
        var nominatorTeam = Team.CreateInternal(leagueId, "Nominator Team", 5000);
        var team1 = Team.CreateInternal(leagueId, "Team 1", 5000);
        var team2 = Team.CreateInternal(leagueId, "Team 2", 5000);
        
        // Verifica che i team hanno gli ID corretti
        Console.WriteLine($"Created Nominator Team ID: {nominatorTeam.Id}");
        Console.WriteLine($"Created Team1 ID: {team1.Id}");
        Console.WriteLine($"Created Team2 ID: {team2.Id}");
        
        // Simula scenario: solo nominatore ha slot per Goalkeeper
        // Team1 e Team2 hanno tutti i loro slot occupati per Goalkeeper (max 3)
        for (int i = 0; i < 3; i++)
        {
            team1.AssignPlayerInternal(PlayerType.Goalkeeper, 10);
            team2.AssignPlayerInternal(PlayerType.Goalkeeper, 10);
        }
        
        // Il nominatore ha solo 2 giocatori, quindi ha 1 slot disponibile
        for (int i = 0; i < 2; i++)
        {
            nominatorTeam.AssignPlayerInternal(PlayerType.Goalkeeper, 10);
        }
        
        var teams = new[] { nominatorTeam, team1, team2 };
        
        // Verifica stato iniziale
        Console.WriteLine($"=== TEAM STATES ===");
        foreach (var team in teams)
        {
            var counts = team.GetPlayerCounts();
            Console.WriteLine($"Team {team.Id}: CountP={counts.P}, HasSlot(Goalkeeper)={team.HasSlot(PlayerType.Goalkeeper)}");
        }
        
        // Act - use the actual ID of nominator team
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorTeam.Id, PlayerType.Goalkeeper);
        
        Console.WriteLine($"=== RESULT ===");
        Console.WriteLine($"AutoAssign: {autoAssign}");
        Console.WriteLine($"EligibleOthers count: {eligibleOthers.Count}");
        foreach (var eligibleId in eligibleOthers)
        {
            Console.WriteLine($"  Eligible: {eligibleId}");
        }
        
        // Assert
        Assert.True(autoAssign, "Should return AutoAssign=true when only nominator has slot");
        Assert.Empty(eligibleOthers);
    }
}
