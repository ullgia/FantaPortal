using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using Xunit;

namespace Tests.Domain;

public class LeagueTests
{
    private static League CreateTestLeague(string name = "Test League")
    {
        return League.Create(name);
    }

    private static SerieAPlayer CreateTestPlayer(int id, PlayerType type, string name = "Test Player")
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
    public void Create_WithValidName_ShouldCreateLeague()
    {
        // Act
        var league = League.Create("My League");

        // Assert
        Assert.Equal("My League", league.Name);
        Assert.Empty(league.Teams);
        Assert.Empty(league.PlayerOwnerships);
        Assert.Null(league.ActiveAuction);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => League.Create(invalidName));
    }

    [Fact]
    public void AddTeam_WithValidData_ShouldAddTeam()
    {
        // Arrange
        var league = CreateTestLeague();

        // Act
        var team = league.AddTeam("Team 1", 500);

        // Assert
        Assert.Single(league.Teams);
        Assert.Equal("Team 1", team.Name);
        Assert.Equal(500, team.Budget);
        Assert.Equal(league.Id, team.LeagueId);
    }

    [Fact]
    public void AddTeam_WithDuplicateName_ShouldThrowException()
    {
        // Arrange
        var league = CreateTestLeague();
        league.AddTeam("Team 1", 500);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => league.AddTeam("Team 1", 300));
        Assert.Contains("Team name already exists", exception.Message);
    }

    [Fact]
    public void AddTeam_WithNegativeBudget_ShouldThrowException()
    {
        // Arrange
        var league = CreateTestLeague();

        // Act & Assert
        Assert.Throws<DomainException>(() => league.AddTeam("Team 1", -100));
    }

    [Fact]
    public void StartAuction_WithValidTeams_ShouldCreateActiveAuction()
    {
        // Arrange
        var league = CreateTestLeague();
        league.AddTeam("Team 1", 500);
        league.AddTeam("Team 2", 500);

        // Act
        league.StartAuction(1, 1);

        // Assert
        Assert.NotNull(league.ActiveAuction);
        Assert.Equal(AuctionStatus.Running, league.ActiveAuction.Status);
        Assert.Equal(1, league.ActiveAuction.BasePrice);
        Assert.Equal(1, league.ActiveAuction.MinIncrement);
        Assert.Equal(2, league.ActiveAuction.TeamOrder.Count);
    }

    [Fact]
    public void StartAuction_WithInsufficientTeams_ShouldThrowException()
    {
        // Arrange
        var league = CreateTestLeague();
        league.AddTeam("Team 1", 500);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => league.StartAuction());
        Assert.Contains("At least 2 teams required", exception.Message);
    }

    [Fact]
    public void StartAuction_WithActiveAuction_ShouldThrowException()
    {
        // Arrange
        var league = CreateTestLeague();
        league.AddTeam("Team 1", 500);
        league.AddTeam("Team 2", 500);
        league.StartAuction();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => league.StartAuction());
        Assert.Contains("Already has an active auction", exception.Message);
    }

    [Fact]
    public void NominatePlayer_AutoAssign_ShouldAssignPlayerAndAdvanceTurn()
    {
        // Arrange
        var league = CreateTestLeague();
        var team1 = league.AddTeam("Team 1", 500);
        var team2 = league.AddTeam("Team 2", 500);
        
        // Simula scenario dove solo nominatore ha slot per portieri
        // (team2 ha già 3 portieri, team1 ne ha 0)
        SetTeamPlayerCounts(team2, 3, 0, 0, 0); // Team2 ha già 3 portieri
        
        league.StartAuction();
        var goalkeeper = CreateTestPlayer(1, PlayerType.Goalkeeper);

        // Act
        var result = league.NominatePlayer(team1.Id, goalkeeper);

        // Assert
        Assert.True(result.IsAutoAssign);
        Assert.False(result.IsReadyCheck);
        Assert.Equal(PlayerType.Goalkeeper, result.Role);
        Assert.Equal(1, result.Price);
        
        // Verifica che il giocatore sia stato assegnato
        var ownership = league.PlayerOwnerships.FirstOrDefault();
        Assert.NotNull(ownership);
        Assert.Equal(team1.Id, ownership.TeamId);
        Assert.Equal(1, ownership.SerieAPlayerId);
        Assert.Equal(1, ownership.PurchasePrice);
        
        // Verifica aggiornamento budget e contatori
        Assert.Equal(499, team1.Budget); // 500 - 1
    }

    [Fact]
    public void NominatePlayer_MultipleEligible_ShouldStartReadyCheck()
    {
        // Arrange
        var league = CreateTestLeague();
        var team1 = league.AddTeam("Team 1", 500);
        var team2 = league.AddTeam("Team 2", 500);
        var team3 = league.AddTeam("Team 3", 500);
        
        league.StartAuction();
        var defender = CreateTestPlayer(200, PlayerType.Defender);

        // Act
        var result = league.NominatePlayer(team1.Id, defender);

        // Assert
        Assert.False(result.IsAutoAssign);
        Assert.True(result.IsReadyCheck);
        Assert.Equal(PlayerType.Defender, result.Role);
        Assert.NotNull(result.ReadyState);
        
        // Verifica che gli altri team siano eligible
        Assert.Contains(team2.Id, result.ReadyState.EligibleTeamIds);
        Assert.Contains(team3.Id, result.ReadyState.EligibleTeamIds);
        Assert.DoesNotContain(team1.Id, result.ReadyState.EligibleTeamIds); // Nominatore non è in eligible
    }

    [Fact]
    public void PlaceBid_WithValidBid_ShouldUpdateBiddingState()
    {
        // Arrange
        var league = CreateTestLeague();
        var team1 = league.AddTeam("Team 1", 500);
        var team2 = league.AddTeam("Team 2", 500);
        league.StartAuction();
        
        var midfielder = CreateTestPlayer(300, PlayerType.Midfielder);
        var nominationResult = league.NominatePlayer(team1.Id, midfielder);
        
        // Completa ready-check e avvia bidding
        league.ConfirmTeamReady(team2.Id);
        league.StartBiddingAfterReady();

        // Act
        league.PlaceBid(team2.Id, 5);

        // Assert
        var biddingInfo = league.ActiveAuction?.GetBiddingInfo();
        Assert.NotNull(biddingInfo);
        Assert.Equal(team2.Id, biddingInfo.HighestBidder);
        Assert.Equal(5, biddingInfo.HighestBid);
    }

    [Fact]
    public void PlaceBid_WithInsufficientBudget_ShouldThrowException()
    {
        // Arrange
        var league = CreateTestLeague();
        var team1 = league.AddTeam("Team 1", 500);
        var team2 = league.AddTeam("Team 2", 10); // Budget basso
        league.StartAuction();
        
        var forward = CreateTestPlayer(400, PlayerType.Forward);
        league.NominatePlayer(team1.Id, forward);
        league.ConfirmTeamReady(team2.Id);
        league.StartBiddingAfterReady();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => league.PlaceBid(team2.Id, 50));
        Assert.Contains("Insufficient budget", exception.Message);
    }

    [Fact]
    public void FinalizeBiddingRound_WithActiveBidding_ShouldAssignPlayerToWinner()
    {
        // Arrange
        var league = CreateTestLeague();
        var team1 = league.AddTeam("Team 1", 500);
        var team2 = league.AddTeam("Team 2", 500);
        league.StartAuction();
        
        var attacker = CreateTestPlayer(500, PlayerType.Forward);
        league.NominatePlayer(team1.Id, attacker);
        league.ConfirmTeamReady(team2.Id);
        league.StartBiddingAfterReady();
        league.PlaceBid(team2.Id, 15);

        // Act
        league.FinalizeBiddingRound(attacker);

        // Assert
        Assert.False(league.ActiveAuction!.IsBiddingActive);
        
        var ownership = league.PlayerOwnerships.FirstOrDefault();
        Assert.NotNull(ownership);
        Assert.Equal(team2.Id, ownership.TeamId);
        Assert.Equal(500, ownership.SerieAPlayerId);
        Assert.Equal(15, ownership.PurchasePrice);
        
        // Verifica aggiornamento budget
        Assert.Equal(485, team2.Budget); // 500 - 15
    }

    [Fact]
    public void GetCurrentAuctionState_WithActiveAuction_ShouldReturnCorrectState()
    {
        // Arrange
        var league = CreateTestLeague();
        var team1 = league.AddTeam("Team 1", 500);
        var team2 = league.AddTeam("Team 2", 400);
        league.StartAuction();

        // Act
        var state = league.GetCurrentAuctionState();

        // Assert
        Assert.Equal(AuctionStatus.Running, state.Status);
        Assert.NotNull(state.CurrentTurn);
        Assert.Equal(2, state.Teams.Count);
        
        var team1Summary = state.Teams.First(t => t.Name == "Team 1");
        Assert.Equal(500, team1Summary.Budget);
        
        var team2Summary = state.Teams.First(t => t.Name == "Team 2");
        Assert.Equal(400, team2Summary.Budget);
    }

    [Fact]
    public void GetTeamsWithSlotForRole_ShouldReturnOnlyEligibleTeams()
    {
        // Arrange
        var league = CreateTestLeague();
        var team1 = league.AddTeam("Team 1", 500);
        var team2 = league.AddTeam("Team 2", 500);
        var team3 = league.AddTeam("Team 3", 500);
        
        // Team2 ha già il massimo di portieri
        SetTeamPlayerCounts(team2, 3, 0, 0, 0);

        // Act
        var eligibleTeams = league.GetTeamsWithSlotForRole(PlayerType.Goalkeeper);

        // Assert
        Assert.Equal(2, eligibleTeams.Count);
        Assert.Contains(team1, eligibleTeams);
        Assert.Contains(team3, eligibleTeams);
        Assert.DoesNotContain(team2, eligibleTeams);
    }

    private static void SetTeamPlayerCounts(Team team, int p, int d, int c, int a)
    {
        // Usa reflection per impostare i contatori per i test
        // In un'implementazione reale, ci sarebbero metodi dedicated per questo
        var type = team.GetType();
        type.GetProperty("CountP")?.SetValue(team, p);
        type.GetProperty("CountD")?.SetValue(team, d);
        type.GetProperty("CountC")?.SetValue(team, c);
        type.GetProperty("CountA")?.SetValue(team, a);
    }
}
