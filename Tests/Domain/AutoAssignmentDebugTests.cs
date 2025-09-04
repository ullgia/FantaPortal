using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Domain.ValueObjects;
using Xunit;

namespace Tests.Domain;

public class AutoAssignmentDebugTests
{
    private static Team CreateTeamWithSlots(Guid id, PlayerType role, int availableSlots)
    {
        var team = Team.CreateInternal(id, $"Team {id}", 5000); // Usa l'ID richiesto!
        
        // Simula l'uso di slot assegnando giocatori usando l'API pubblica
        var maxSlots = role switch
        {
            PlayerType.Goalkeeper => 3,  // Max 3 portieri
            PlayerType.Defender => 8,    // Max 8 difensori
            PlayerType.Midfielder => 8,  // Max 8 centrocampisti
            PlayerType.Forward => 6,     // Max 6 attaccanti
            _ => 0
        };

        var usedSlots = maxSlots - availableSlots;

        // Usa AssignPlayerInternal per simulare giocatori già assegnati
        for (int i = 0; i < usedSlots; i++)
        {
            try
            {
                team.AssignPlayerInternal(role, 10); // Prezzo fisso per test
            }
            catch
            {
                // Se non può assegnare più giocatori, fermati
                break;
            }
        }
        
        return team;
    }

    private static SerieAPlayer CreateSerieAPlayer(int id, PlayerType type, string name = "Test Player")
    {
        var role = type switch
        {
            PlayerType.Goalkeeper => "P",
            PlayerType.Defender => "D", 
            PlayerType.Midfielder => "C",
            PlayerType.Forward => "A",
            _ => throw new ArgumentException($"Unsupported PlayerType: {type}")
        };
        
        return SerieAPlayer.Create(id, role, role, name, "Test Team", 10m, 10m, 10);
    }

    [Fact]
    public void DebugCreateTeamWithSlotsHelper()
    {
        Console.WriteLine("=== DEBUG CreateTeamWithSlots HELPER ===");
        
        // Test che simula il test che fallisce - team2 dovrebbe avere 0 slot
        var team2Id = Guid.NewGuid();
        var team2 = CreateTeamWithSlots(team2Id, PlayerType.Defender, 0);
        
        Console.WriteLine($"Team2 ID: {team2.Id}");
        Console.WriteLine($"Team2 MaxD: {team2.GetType().GetProperty("MaxD")?.GetValue(team2)}");
        Console.WriteLine($"Team2 CountD: {team2.GetType().GetProperty("CountD")?.GetValue(team2)}");
        Console.WriteLine($"Team2 AvailableSlots(Defender): {team2.GetAvailableSlots(PlayerType.Defender)}");
        Console.WriteLine($"Team2 HasSlot(Defender): {team2.HasSlot(PlayerType.Defender)}");
        
        // Test anche un team che dovrebbe avere slot
        var team1Id = Guid.NewGuid();
        var team1 = CreateTeamWithSlots(team1Id, PlayerType.Defender, 1);
        
        Console.WriteLine($"Team1 ID: {team1.Id}");
        Console.WriteLine($"Team1 MaxD: {team1.GetType().GetProperty("MaxD")?.GetValue(team1)}");
        Console.WriteLine($"Team1 CountD: {team1.GetType().GetProperty("CountD")?.GetValue(team1)}");
        Console.WriteLine($"Team1 AvailableSlots(Defender): {team1.GetAvailableSlots(PlayerType.Defender)}");
        Console.WriteLine($"Team1 HasSlot(Defender): {team1.HasSlot(PlayerType.Defender)}");
        
        Assert.False(team2.HasSlot(PlayerType.Defender), "Team2 should NOT have available slots");
        Assert.True(team1.HasSlot(PlayerType.Defender), "Team1 should have available slots");
    }

    [Fact]
    public void TestActualAutoAssignmentLogic()
    {
        // Arrange - Create teams where only one has slots
        var nominatorTeam = CreateTeamWithSlots(Guid.NewGuid(), PlayerType.Goalkeeper, 1); // Ha 1 slot
        var team1 = CreateTeamWithSlots(Guid.NewGuid(), PlayerType.Goalkeeper, 0);              // 0 slot
        var team2 = CreateTeamWithSlots(Guid.NewGuid(), PlayerType.Goalkeeper, 0);              // 0 slot
        
        var teams = new[] { nominatorTeam, team1, team2 };

        // Verify team states
        Assert.True(nominatorTeam.HasSlot(PlayerType.Goalkeeper));
        Assert.False(team1.HasSlot(PlayerType.Goalkeeper));
        Assert.False(team2.HasSlot(PlayerType.Goalkeeper));
        
        // Act
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorTeam.Id, PlayerType.Goalkeeper);

        // Debug output
        Console.WriteLine($"Nominator ID: {nominatorTeam.Id}");
        Console.WriteLine($"Team1 ID: {team1.Id}");
        Console.WriteLine($"Team2 ID: {team2.Id}");
        Console.WriteLine($"AutoAssign: {autoAssign}");
        Console.WriteLine($"EligibleOthers Count: {eligibleOthers.Count}");
        foreach (var eligibleId in eligibleOthers)
        {
            Console.WriteLine($"  Eligible: {eligibleId}");
        }

        // Assert
        Assert.True(autoAssign, "Should return auto-assign when only nominator has slot");
        Assert.Empty(eligibleOthers);
    }
}
