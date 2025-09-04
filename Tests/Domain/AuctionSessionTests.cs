using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using Xunit;

namespace Tests.Domain;

public class AuctionSessionTests
{
    private static Team CreateTeamWithSlots(string name, PlayerType role, int availableSlots)
    {
        // Crea un team con slot disponibili specifici per il test
        var leagueId = Guid.NewGuid();
        var team = Team.CreateInternal(leagueId, name, 5000);
        
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
    public void ProcessNomination_WhenOnlyNominatorHasSlot_ShouldReturnAutoAssign()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });
        session.Start();
        
        var nominatorTeam = CreateTeamWithSlots("Nominator", PlayerType.Goalkeeper, 1); // Ha slot
        var otherTeam = CreateTeamWithSlots("Other", PlayerType.Goalkeeper, 0);      // Non ha slot
        
        var teams = new Dictionary<Guid, Team>
        {
            { nominatorTeam.Id, nominatorTeam },
            { otherTeam.Id, otherTeam }
        };
        
        var player = CreateSerieAPlayer(1, PlayerType.Goalkeeper);

        // Act
        var result = session.ProcessNomination(nominatorTeam.Id, player, teams);

        // Assert
        Assert.True(result.IsAutoAssign);
        Assert.False(result.IsReadyCheck);
        Assert.Equal(PlayerType.Goalkeeper, result.Role);
        Assert.Equal(1, result.Price); // BasePrice
        Assert.Null(result.BiddingInfo);
        Assert.Null(result.ReadyState);
    }

    [Fact]
    public void ProcessNomination_WhenMultipleTeamsHaveSlots_ShouldStartReadyCheck()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });
        session.Start();
        
        var nominatorTeam = CreateTeamWithSlots("Nominator", PlayerType.Defender, 1);
        var team2 = CreateTeamWithSlots("Team2", PlayerType.Defender, 1);
        var team3 = CreateTeamWithSlots("Team3", PlayerType.Defender, 0); // Non ha slot
        
        var teams = new Dictionary<Guid, Team>
        {
            { nominatorTeam.Id, nominatorTeam },
            { team2.Id, team2 },
            { team3.Id, team3 }
        };
        
        var player = CreateSerieAPlayer(200, PlayerType.Defender);

        // Act
        var result = session.ProcessNomination(nominatorTeam.Id, player, teams);

        // Assert
        Assert.False(result.IsAutoAssign);
        Assert.True(result.IsReadyCheck);
        Assert.Equal(PlayerType.Defender, result.Role);
        Assert.NotNull(result.ReadyState);
        Assert.Equal(nominatorTeam.Id, result.ReadyState.NominatorTeamId);
        Assert.Equal(200, result.ReadyState.SerieAPlayerId);
        Assert.Contains(team2.Id, result.ReadyState.EligibleTeamIds); // Solo team2 è eligible
        Assert.DoesNotContain(team3.Id, result.ReadyState.EligibleTeamIds); // team3 non ha slot
    }

    [Fact]
    public void ConfirmTeamReady_WhenAllTeamsReady_ShouldCompletReadyCheck()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid>());
        session.Start();
        
        var nominatorId = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var eligibleTeams = new List<Guid> { team2Id };
        
        var readyState = session.StartReadyCheck(nominatorId, 300, PlayerType.Midfielder, eligibleTeams);

        // Act - Team2 conferma ready
        var confirmed = session.ConfirmTeamReady(team2Id);

        // Assert
        Assert.True(confirmed);
        Assert.True(readyState.AllTeamsReady);
        Assert.True(readyState.IsCompleted);
    }

    [Fact]
    public void StartBiddingAfterReady_WhenReadyCompleted_ShouldReturnBiddingInfo()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid>());
        session.Start();
        
        var nominatorId = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var eligibleTeams = new List<Guid> { team2Id };
        
        Console.WriteLine($"=== DEBUG StartBiddingAfterReady_WhenReadyCompleted_ShouldReturnBiddingInfo ===");
        Console.WriteLine($"NominatorId: {nominatorId}");
        Console.WriteLine($"Team2Id: {team2Id}");
        
        var readyState = session.StartReadyCheck(nominatorId, 400, PlayerType.Forward, eligibleTeams);
        Console.WriteLine($"ReadyState created. EligibleTeamIds: [{string.Join(", ", readyState.EligibleTeamIds)}]");
        Console.WriteLine($"ReadyState AllTeamsReady before confirm: {readyState.AllTeamsReady}");
        
        var confirmResult = session.ConfirmTeamReady(team2Id); // Completa ready-check
        Console.WriteLine($"ConfirmTeamReady result: {confirmResult}");
        Console.WriteLine($"ReadyState AllTeamsReady after confirm: {readyState.AllTeamsReady}");
        Console.WriteLine($"ReadyState IsCompleted: {readyState.IsCompleted}");

        // Act
        var biddingInfo = session.StartBiddingAfterReady();

        // Assert
        Assert.NotNull(biddingInfo);
        Assert.Equal(nominatorId, biddingInfo.NominatorId);
        Assert.Equal(400, biddingInfo.PlayerId);
        Assert.Equal(nominatorId, biddingInfo.HighestBidder); // Inizia con nominatore
        Assert.Equal(1, biddingInfo.HighestBid); // BasePrice
        Assert.Contains(nominatorId, biddingInfo.EligibleTeams);
        Assert.Contains(team2Id, biddingInfo.EligibleTeams);
    }

    [Fact]
    public void PlaceBid_WhenValidBid_ShouldUpdateBiddingState()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid>());
        session.Start();
        
        var nominatorId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var eligibleTeams = new List<Guid> { bidderId };
        
        session.StartReadyCheck(nominatorId, 500, PlayerType.Goalkeeper, eligibleTeams);
        session.ConfirmTeamReady(bidderId);
        session.StartBiddingAfterReady();

        // Act
        var bidResult = session.PlaceBid(bidderId, 5);

        // Assert
        Assert.Equal(5, bidResult.Amount);
        
        var biddingInfo = session.GetBiddingInfo();
        Assert.NotNull(biddingInfo);
        Assert.Equal(bidderId, biddingInfo.HighestBidder);
        Assert.Equal(5, biddingInfo.HighestBid);
    }

    [Fact]
    public void PlaceBid_WhenBidTooLow_ShouldThrowException()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid>());
        session.Start();
        
        var nominatorId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();

        session.StartReadyCheck(nominatorId, 600, PlayerType.Midfielder, new List<Guid> { bidderId });
        session.ConfirmTeamReady(bidderId);
        session.StartBiddingAfterReady();
        session.PlaceBid(bidderId, 5); // Prima offerta valida

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => session.PlaceBid(nominatorId, 5)); // Stessa offerta
        Assert.Contains("Bid too low", exception.Message);
    }

    [Fact]
    public void GetWinningBid_WhenBiddingActive_ShouldReturnHighestBid()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid>());
        session.Start();
        
        var nominatorId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();

        session.StartReadyCheck(nominatorId, 700, PlayerType.Forward, new List<Guid> { bidderId });
        session.ConfirmTeamReady(bidderId);
        session.StartBiddingAfterReady();
        session.PlaceBid(bidderId, 10);

        // Act
        var winningBid = session.GetWinningBid();

        // Assert
        Assert.Equal(bidderId, winningBid.TeamId);
        Assert.Equal(10, winningBid.Amount);
    }

    [Fact]
    public void FinalizeBidding_WhenBiddingActive_ShouldClearBiddingAndAdvanceTurn()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, 
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });
        session.Start();
        
        var team1 = CreateTeamWithSlots("Team1", PlayerType.Goalkeeper, 1);
        var team2 = CreateTeamWithSlots("Team2", PlayerType.Goalkeeper, 1);
        
        var teams = new Dictionary<Guid, Team>
        {
            { team1.Id, team1 },
            { team2.Id, team2 }
        };
        
        // Simula bidding attivo
        var nominatorId = team1.Id;
        session.StartReadyCheck(nominatorId, 800, PlayerType.Goalkeeper, new List<Guid> { team2.Id });
        session.ConfirmTeamReady(team2.Id);
        session.StartBiddingAfterReady();

        // Act
        var nextTurn = session.FinalizeBidding(teams);

        // Assert
        Assert.NotNull(nextTurn);
        Assert.False(session.IsBiddingActive);
    }
}
