using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Xunit;

namespace Tests.Domain;

public class AuctionFlowTests
{
    private static Team CreateTeamWithSlots(PlayerType role, int availableSlots)
    {
        var leagueId = Guid.NewGuid(); // ID della lega
        var team = Team.CreateInternal(leagueId, $"TestTeam", 5000);
        
        Console.WriteLine($"=== DEBUG CreateTeamWithSlots for {role}, want {availableSlots} available ===");
        Console.WriteLine($"Team initial: Budget={team.Budget}");
        Console.WriteLine($"  Goalkeeper: Count={team.GetPlayerCounts().P}, Available={team.GetAvailableSlots(PlayerType.Goalkeeper)}, HasSlot={team.HasSlot(PlayerType.Goalkeeper)}");
        Console.WriteLine($"  Defender: Count={team.GetPlayerCounts().D}, Available={team.GetAvailableSlots(PlayerType.Defender)}, HasSlot={team.HasSlot(PlayerType.Defender)}");
        Console.WriteLine($"  Midfielder: Count={team.GetPlayerCounts().C}, Available={team.GetAvailableSlots(PlayerType.Midfielder)}, HasSlot={team.HasSlot(PlayerType.Midfielder)}");
        Console.WriteLine($"  Forward: Count={team.GetPlayerCounts().A}, Available={team.GetAvailableSlots(PlayerType.Forward)}, HasSlot={team.HasSlot(PlayerType.Forward)}");
        
        // Prima riempi TUTTI gli altri ruoli al massimo
        var allRoles = new[] { PlayerType.Goalkeeper, PlayerType.Defender, PlayerType.Midfielder, PlayerType.Forward };
        
        foreach (var otherRole in allRoles)
        {
            if (otherRole == role) continue; // Salta il ruolo che vogliamo configurare
            
            var maxSlotsOther = otherRole switch
            {
                PlayerType.Goalkeeper => 3,
                PlayerType.Defender => 8,
                PlayerType.Midfielder => 8,
                PlayerType.Forward => 6,
                _ => 0
            };
            
            Console.WriteLine($"Filling {otherRole} to max ({maxSlotsOther} slots)...");
            // Riempi al massimo tutti gli altri ruoli
            for (int i = 0; i < maxSlotsOther; i++)
            {
                try
                {
                    team.AssignPlayerInternal(otherRole, 10);
                    Console.WriteLine($"  ✓ Filled {otherRole} slot {i+1}/{maxSlotsOther}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed to fill {otherRole} slot {i+1}: {ex.Message}");
                    break;
                }
            }
            Console.WriteLine($"  {otherRole} final: Available={team.GetAvailableSlots(otherRole)}, HasSlot={team.HasSlot(otherRole)}");
        }
        
        // Ora configura il ruolo target
        var maxSlots = role switch
        {
            PlayerType.Goalkeeper => 3,  // Max 3 portieri
            PlayerType.Defender => 8,    // Max 8 difensori
            PlayerType.Midfielder => 8,  // Max 8 centrocampisti
            PlayerType.Forward => 6,     // Max 6 attaccanti
            _ => 0
        };

        var usedSlots = maxSlots - availableSlots;
        Console.WriteLine($"Target role {role}: Max={maxSlots}, want available={availableSlots}, will use={usedSlots}");

        // Usa AssignPlayerInternal per simulare giocatori già assegnati per il ruolo target
        for (int i = 0; i < usedSlots; i++)
        {
            try
            {
                team.AssignPlayerInternal(role, 10); // Prezzo fisso per test
                Console.WriteLine($"  ✓ Assigned target player {i+1} for {role}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed to assign target player {i+1} for {role}: {ex.Message}");
                break;
            }
        }
        
        Console.WriteLine($"=== FINAL TEAM STATE ===");
        Console.WriteLine($"  Goalkeeper: Count={team.GetPlayerCounts().P}, Available={team.GetAvailableSlots(PlayerType.Goalkeeper)}, HasSlot={team.HasSlot(PlayerType.Goalkeeper)}");
        Console.WriteLine($"  Defender: Count={team.GetPlayerCounts().D}, Available={team.GetAvailableSlots(PlayerType.Defender)}, HasSlot={team.HasSlot(PlayerType.Defender)}");
        Console.WriteLine($"  Midfielder: Count={team.GetPlayerCounts().C}, Available={team.GetAvailableSlots(PlayerType.Midfielder)}, HasSlot={team.HasSlot(PlayerType.Midfielder)}");
        Console.WriteLine($"  Forward: Count={team.GetPlayerCounts().A}, Available={team.GetAvailableSlots(PlayerType.Forward)}, HasSlot={team.HasSlot(PlayerType.Forward)}");
        Console.WriteLine($"Final budget: {team.Budget}");
        return team;
    }

    [Fact]
    public void EvaluateNomination_WhenOnlyNominatorHasSlot_ShouldReturnAutoAssign()
    {
        // Arrange
        var nominatorTeam = CreateTeamWithSlots(PlayerType.Goalkeeper, 1); // Nominatore ha slot
        var team1 = CreateTeamWithSlots(PlayerType.Goalkeeper, 0);         // Team1 non ha slot
        var team2 = CreateTeamWithSlots(PlayerType.Goalkeeper, 0);         // Team2 non ha slot
        
        var teams = new[] { nominatorTeam, team1, team2 };

        // Act
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorTeam.Id, PlayerType.Goalkeeper);

        // Assert
        Assert.True(autoAssign);
        Assert.Empty(eligibleOthers);
    }

    [Fact]
    public void EvaluateNomination_WhenMultipleTeamsHaveSlots_ShouldReturnEligibleOthers()
    {
        // Arrange
        var nominatorTeam = CreateTeamWithSlots(PlayerType.Defender, 2); // Nominatore ha slot
        var team1 = CreateTeamWithSlots(PlayerType.Defender, 1);         // Team1 ha slot
        var team2 = CreateTeamWithSlots(PlayerType.Defender, 0);         // Team2 non ha slot
        var team3 = CreateTeamWithSlots(PlayerType.Defender, 3);         // Team3 ha slot
        
        var teams = new[] { nominatorTeam, team1, team2, team3 };

        // Act
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorTeam.Id, PlayerType.Defender);

        // Assert
        Assert.False(autoAssign);
        Assert.Equal(2, eligibleOthers.Count);
        Assert.Contains(team1.Id, eligibleOthers); // Team1 è eligible
        Assert.Contains(team3.Id, eligibleOthers); // Team3 è eligible
        Assert.DoesNotContain(team2.Id, eligibleOthers); // Team2 non ha slot
        Assert.DoesNotContain(nominatorTeam.Id, eligibleOthers); // Nominatore non è in eligible
    }

    [Fact]
    public void EvaluateNomination_WhenNoTeamsHaveSlots_ShouldReturnAutoAssign()
    {
        // Arrange
        var nominatorTeam = CreateTeamWithSlots(PlayerType.Midfielder, 1); // Solo nominatore ha slot
        var team1 = CreateTeamWithSlots(PlayerType.Midfielder, 0);         // Altri non hanno slot
        
        var teams = new[] { nominatorTeam, team1 };

        // Act
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorTeam.Id, PlayerType.Midfielder);

        // Assert
        Assert.True(autoAssign);
        Assert.Empty(eligibleOthers);
    }

    [Fact]
    public void AdvanceUntilEligible_ShouldFollowCircularLogicWithinRole()
    {
        // Arrange
        var team1 = CreateTeamWithSlots(PlayerType.Goalkeeper, 1);
        var team2 = CreateTeamWithSlots(PlayerType.Goalkeeper, 0); // Non ha slot
        var team3 = CreateTeamWithSlots(PlayerType.Goalkeeper, 1);
        
        var teamOrder = new List<Guid> { team1.Id, team2.Id, team3.Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1.Id, team1 },
            { team2.Id, team2 },
            { team3.Id, team3 }
        };

        // Act - Dal team 1 (index 0), dovrebbe andare al team 3 (index 2) perché team 2 non ha slot
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Goalkeeper, 0);

        // Assert
        Assert.Equal(PlayerType.Goalkeeper, nextRole);
        Assert.Equal(2, nextIndex); // Team3 è il prossimo eligible
    }

    [Fact]
    public void AdvanceUntilEligible_ShouldWrapAroundInCircularFashion()
    {
        // Arrange
        var team1 = CreateTeamWithSlots(PlayerType.Defender, 1);
        var team2 = CreateTeamWithSlots(PlayerType.Defender, 0); // Non ha slot
        var team3 = CreateTeamWithSlots(PlayerType.Defender, 0); // Non ha slot
        
        var teamOrder = new List<Guid> { team1.Id, team2.Id, team3.Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1.Id, team1 },
            { team2.Id, team2 },
            { team3.Id, team3 }
        };

        // Act - Dal team 3 (index 2), dovrebbe tornare al team 1 (index 0)
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Defender, 2);

        // Assert
        Assert.Equal(PlayerType.Defender, nextRole);
        Assert.Equal(0, nextIndex); // Torna al team1
    }

    [Fact]
    public void AdvanceUntilEligible_WhenNoMoreSlotsInCurrentRole_ShouldAdvanceRole()
    {
        Console.WriteLine("=== TEST AdvanceUntilEligible_WhenNoMoreSlotsInCurrentRole_ShouldAdvanceRole START ===");
        // Arrange - Vogliamo testare avanzamento da P a D
        // Team dovrebbero avere slot per D ma non per P
        var team1 = CreateTeamWithSlots(PlayerType.Defender, 1); // Ha 1 slot D, nessun slot P
        var team2 = CreateTeamWithSlots(PlayerType.Defender, 1); // Ha 1 slot D, nessun slot P
        
        Console.WriteLine($"=== CREATED TEAMS ===");
        Console.WriteLine($"Team1 ID: {team1.Id}");
        Console.WriteLine($"Team2 ID: {team2.Id}");
        
        var teamOrder = new List<Guid> { team1.Id, team2.Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1.Id, team1 },
            { team2.Id, team2 }
        };

        // Act - Da P dovrebbe avanzare a D
        // DEBUG: Verifichiamo lo stato dei team prima della chiamata
        Console.WriteLine($"=== STATE CHECK FOR AdvanceUntilEligible_WhenNoMoreSlotsInCurrentRole_ShouldAdvanceRole ===");
        Console.WriteLine($"Team1 hasSlot(Goalkeeper): {team1.HasSlot(PlayerType.Goalkeeper)}");
        Console.WriteLine($"Team2 hasSlot(Goalkeeper): {team2.HasSlot(PlayerType.Goalkeeper)}");
        Console.WriteLine($"Team1 hasSlot(Defender): {team1.HasSlot(PlayerType.Defender)}");
        Console.WriteLine($"Team2 hasSlot(Defender): {team2.HasSlot(PlayerType.Defender)}");
        Console.WriteLine($"=== CALLING AdvanceUntilEligible ===");
        
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Goalkeeper, 0);

        Console.WriteLine($"=== RESULT: nextRole={nextRole}, nextIndex={nextIndex} ===");
        // Assert
        Assert.Equal(PlayerType.Defender, nextRole);
        Assert.Equal(0, nextIndex); // Inizia dal primo team nel nuovo ruolo
    }

    [Fact]
    public void AdvanceUntilEligible_WhenAllRolesCompleted_ShouldReturnNull()
    {
        // Arrange
        var team1 = CreateTeamWithSlots(PlayerType.Forward, 0); // Nessun slot rimasto
        var team2 = CreateTeamWithSlots(PlayerType.Forward, 0); // Nessun slot rimasto
        
        var teamOrder = new List<Guid> { team1.Id, team2.Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1.Id, team1 },
            { team2.Id, team2 }
        };

        // Act - Da ultimo ruolo (A) senza slot disponibili
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Forward, 0);

        // Assert
        Assert.Null(nextRole); // Asta completata
        Assert.Equal(0, nextIndex); // Index rimane invariato
    }

    [Fact]
    public void AdvanceUntilEligible_RoleProgression_ShouldFollowCorrectOrder()
    {
        // Arrange & Test progression P -> D -> C -> A
        var teamOrder = new List<Guid>();
        var teams = new Dictionary<Guid, Team>();
        
        // Slot available for D only
        var team1 = CreateTeamWithSlots(PlayerType.Defender, 1);
        teamOrder.Add(team1.Id);
        teams[team1.Id] = team1;
        
        var (role1, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Goalkeeper, 0);
        Assert.Equal(PlayerType.Defender, role1);

        // Create new team with slot for C only 
        var team2 = CreateTeamWithSlots(PlayerType.Midfielder, 1);
        teamOrder.Clear();
        teamOrder.Add(team2.Id);
        teams.Clear();
        teams[team2.Id] = team2;
        
        var (role2, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Defender, 0);
        Assert.Equal(PlayerType.Midfielder, role2);

        // Create new team with slot for A only
        var team3 = CreateTeamWithSlots(PlayerType.Forward, 1);
        teamOrder.Clear();
        teamOrder.Add(team3.Id);
        teams.Clear();
        teams[team3.Id] = team3;
        
        var (role3, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Midfielder, 0);
        Assert.Equal(PlayerType.Forward, role3);

        // Create new team with no slots for A
        var team4 = CreateTeamWithSlots(PlayerType.Forward, 0);
        teamOrder.Clear();
        teamOrder.Add(team4.Id);
        teams.Clear();
        teams[team4.Id] = team4;
        
        var (role4, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Forward, 0);
        Assert.Null(role4);
    }

    [Fact]
    public void CircularAdvancement_ComplexScenario()
    {
        // Arrange - Scenario più complesso con 4 team
        var team1 = CreateTeamWithSlots(PlayerType.Midfielder, 0); // Non ha slot
        var team2 = CreateTeamWithSlots(PlayerType.Midfielder, 1); // Ha slot
        var team3 = CreateTeamWithSlots(PlayerType.Midfielder, 0); // Non ha slot
        var team4 = CreateTeamWithSlots(PlayerType.Midfielder, 1); // Ha slot
        
        var teamOrder = new List<Guid> { team1.Id, team2.Id, team3.Id, team4.Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1.Id, team1 },
            { team2.Id, team2 },
            { team3.Id, team3 },
            { team4.Id, team4 }
        };

        // Act - Dal team1 (index 0), dovrebbe andare al team2 (index 1)
        var (role1, index1) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Midfielder, 0);
        Assert.Equal(PlayerType.Midfielder, role1);
        Assert.Equal(1, index1);

        // Dal team2 (index 1), dovrebbe andare al team4 (index 3)
        var (role2, index2) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Midfielder, 1);
        Assert.Equal(PlayerType.Midfielder, role2);
        Assert.Equal(3, index2);

        // Dal team4 (index 3), dovrebbe tornare al team2 (index 1) - circular
        var (role3, index3) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, PlayerType.Midfielder, 3);
        Assert.Equal(PlayerType.Midfielder, role3);
        Assert.Equal(1, index3);
    }
}
