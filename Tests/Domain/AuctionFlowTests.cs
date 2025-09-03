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
    private static Team CreateTeamWithSlots(Guid id, RoleType role, int availableSlots)
    {
        var team = Team.CreateInternal(Guid.NewGuid(), $"Team {id}", 500);
        
        // Simula l'uso di slot impostando i contatori per riflettere gli slot disponibili
        var usedSlots = role switch
        {
            RoleType.P => 3 - availableSlots,  // Max 3 portieri
            RoleType.D => 8 - availableSlots,  // Max 8 difensori
            RoleType.C => 8 - availableSlots,  // Max 8 centrocampisti
            RoleType.A => 6 - availableSlots,  // Max 6 attaccanti
            _ => 0
        };

        // Usa reflection per impostare i contatori (solo per test)
        var property = role switch
        {
            RoleType.P => "CountP",
            RoleType.D => "CountD",
            RoleType.C => "CountC",
            RoleType.A => "CountA",
            _ => throw new ArgumentException("Invalid role")
        };
        
        team.GetType().GetProperty(property)?.SetValue(team, usedSlots);
        return team;
    }

    [Fact]
    public void EvaluateNomination_WhenOnlyNominatorHasSlot_ShouldReturnAutoAssign()
    {
        // Arrange
        var nominatorId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        
        var teams = new[]
        {
            CreateTeamWithSlots(nominatorId, RoleType.P, 1), // Nominatore ha slot
            CreateTeamWithSlots(team1Id, RoleType.P, 0),     // Team1 non ha slot
            CreateTeamWithSlots(team2Id, RoleType.P, 0)      // Team2 non ha slot
        };

        // Act
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorId, RoleType.P);

        // Assert
        Assert.True(autoAssign);
        Assert.Empty(eligibleOthers);
    }

    [Fact]
    public void EvaluateNomination_WhenMultipleTeamsHaveSlots_ShouldReturnEligibleOthers()
    {
        // Arrange
        var nominatorId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        
        var teams = new[]
        {
            CreateTeamWithSlots(nominatorId, RoleType.D, 2), // Nominatore ha slot
            CreateTeamWithSlots(team1Id, RoleType.D, 1),     // Team1 ha slot
            CreateTeamWithSlots(team2Id, RoleType.D, 0),     // Team2 non ha slot
            CreateTeamWithSlots(team3Id, RoleType.D, 3)      // Team3 ha slot
        };

        // Act
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorId, RoleType.D);

        // Assert
        Assert.False(autoAssign);
        Assert.Equal(2, eligibleOthers.Count);
        Assert.Contains(team1Id, eligibleOthers); // Team1 è eligible
        Assert.Contains(team3Id, eligibleOthers); // Team3 è eligible
        Assert.DoesNotContain(team2Id, eligibleOthers); // Team2 non ha slot
        Assert.DoesNotContain(nominatorId, eligibleOthers); // Nominatore non è in eligible
    }

    [Fact]
    public void EvaluateNomination_WhenNoTeamsHaveSlots_ShouldReturnAutoAssign()
    {
        // Arrange
        var nominatorId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        
        var teams = new[]
        {
            CreateTeamWithSlots(nominatorId, RoleType.C, 1), // Solo nominatore ha slot
            CreateTeamWithSlots(team1Id, RoleType.C, 0)      // Altri non hanno slot
        };

        // Act
        var (autoAssign, eligibleOthers) = AuctionFlow.EvaluateNomination(teams, nominatorId, RoleType.C);

        // Assert
        Assert.True(autoAssign);
        Assert.Empty(eligibleOthers);
    }

    [Fact]
    public void AdvanceUntilEligible_ShouldFollowCircularLogicWithinRole()
    {
        // Arrange
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        var teamOrder = new List<Guid> { team1Id, team2Id, team3Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1Id, CreateTeamWithSlots(team1Id, RoleType.P, 1) },
            { team2Id, CreateTeamWithSlots(team2Id, RoleType.P, 0) }, // Non ha slot
            { team3Id, CreateTeamWithSlots(team3Id, RoleType.P, 1) }
        };

        // Act - Dal team 1 (index 0), dovrebbe andare al team 3 (index 2) perché team 2 non ha slot
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.P, 0);

        // Assert
        Assert.Equal(RoleType.P, nextRole);
        Assert.Equal(2, nextIndex); // Team3 è il prossimo eligible
    }

    [Fact]
    public void AdvanceUntilEligible_ShouldWrapAroundInCircularFashion()
    {
        // Arrange
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        var teamOrder = new List<Guid> { team1Id, team2Id, team3Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1Id, CreateTeamWithSlots(team1Id, RoleType.D, 1) },
            { team2Id, CreateTeamWithSlots(team2Id, RoleType.D, 0) }, // Non ha slot
            { team3Id, CreateTeamWithSlots(team3Id, RoleType.D, 0) }  // Non ha slot
        };

        // Act - Dal team 3 (index 2), dovrebbe tornare al team 1 (index 0)
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.D, 2);

        // Assert
        Assert.Equal(RoleType.D, nextRole);
        Assert.Equal(0, nextIndex); // Torna al team1
    }

    [Fact]
    public void AdvanceUntilEligible_WhenNoMoreSlotsInCurrentRole_ShouldAdvanceRole()
    {
        // Arrange
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var teamOrder = new List<Guid> { team1Id, team2Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1Id, CreateTeamWithSlots(team1Id, RoleType.P, 0) }, // Non ha slot P
            { team2Id, CreateTeamWithSlots(team2Id, RoleType.P, 0) }  // Non ha slot P
        };
        
        // Imposta slot disponibili per difensori
        teams[team1Id] = CreateTeamWithSlots(team1Id, RoleType.D, 1);
        teams[team2Id] = CreateTeamWithSlots(team2Id, RoleType.D, 1);

        // Act - Da P dovrebbe avanzare a D
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.P, 0);

        // Assert
        Assert.Equal(RoleType.D, nextRole);
        Assert.Equal(0, nextIndex); // Inizia dal primo team nel nuovo ruolo
    }

    [Fact]
    public void AdvanceUntilEligible_WhenAllRolesCompleted_ShouldReturnNull()
    {
        // Arrange
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var teamOrder = new List<Guid> { team1Id, team2Id };
        
        // Tutti i team non hanno più slot per nessun ruolo
        var teams = new Dictionary<Guid, Team>
        {
            { team1Id, CreateTeamWithSlots(team1Id, RoleType.A, 0) }, // Nessun slot rimasto
            { team2Id, CreateTeamWithSlots(team2Id, RoleType.A, 0) }  // Nessun slot rimasto
        };

        // Act - Da ultimo ruolo (A) senza slot disponibili
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.A, 0);

        // Assert
        Assert.Null(nextRole); // Asta completata
        Assert.Equal(0, nextIndex); // Index rimane invariato
    }

    [Fact]
    public void AdvanceUntilEligible_RoleProgression_ShouldFollowCorrectOrder()
    {
        // Arrange
        var team1Id = Guid.NewGuid();
        var teamOrder = new List<Guid> { team1Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1Id, CreateTeamWithSlots(team1Id, RoleType.P, 0) } // No slots for P
        };

        // Test progression P -> D -> C -> A
        
        // Slot available for D
        teams[team1Id] = CreateTeamWithSlots(team1Id, RoleType.D, 1);
        var (role1, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.P, 0);
        Assert.Equal(RoleType.D, role1);

        // No slot for D, slot available for C
        teams[team1Id] = CreateTeamWithSlots(team1Id, RoleType.C, 1);
        var (role2, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.D, 0);
        Assert.Equal(RoleType.C, role2);

        // No slot for C, slot available for A
        teams[team1Id] = CreateTeamWithSlots(team1Id, RoleType.A, 1);
        var (role3, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.C, 0);
        Assert.Equal(RoleType.A, role3);

        // No slot for A, should return null
        teams[team1Id] = CreateTeamWithSlots(team1Id, RoleType.A, 0);
        var (role4, _) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.A, 0);
        Assert.Null(role4);
    }

    [Fact]
    public void CircularAdvancement_ComplexScenario()
    {
        // Arrange - Scenario più complesso con 4 team
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        var team4Id = Guid.NewGuid();
        var teamOrder = new List<Guid> { team1Id, team2Id, team3Id, team4Id };
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1Id, CreateTeamWithSlots(team1Id, RoleType.C, 0) }, // Non ha slot
            { team2Id, CreateTeamWithSlots(team2Id, RoleType.C, 1) }, // Ha slot
            { team3Id, CreateTeamWithSlots(team3Id, RoleType.C, 0) }, // Non ha slot
            { team4Id, CreateTeamWithSlots(team4Id, RoleType.C, 1) }  // Ha slot
        };

        // Act - Dal team1 (index 0), dovrebbe andare al team2 (index 1)
        var (role1, index1) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.C, 0);
        Assert.Equal(RoleType.C, role1);
        Assert.Equal(1, index1);

        // Dal team2 (index 1), dovrebbe andare al team4 (index 3)
        var (role2, index2) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.C, 1);
        Assert.Equal(RoleType.C, role2);
        Assert.Equal(3, index2);

        // Dal team4 (index 3), dovrebbe tornare al team2 (index 1) - circular
        var (role3, index3) = AuctionFlow.AdvanceUntilEligible(teamOrder, teams, RoleType.C, 3);
        Assert.Equal(RoleType.C, role3);
        Assert.Equal(1, index3);
    }
}
