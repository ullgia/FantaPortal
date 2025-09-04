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
    private static Team CreateTeamWithSlots(Guid id, string name, PlayerType role, int availableSlots)
    {
        // Crea un team con slot disponibili specifici per il test
        var team = Team.CreateInternal(Guid.NewGuid(), name, 500);
        
        // Simula l'uso di slot impostando i contatori
        // Nota: questo usa reflection o metodi interni per test purposes
        switch (role)
        {
            case PlayerType.Goalkeeper:
                team.GetType().GetProperty("CountP")?.SetValue(team, 3 - availableSlots);
                break;
            case PlayerType.Defender:
                team.GetType().GetProperty("CountD")?.SetValue(team, 8 - availableSlots);
                break;
            case PlayerType.Midfielder:
                team.GetType().GetProperty("CountC")?.SetValue(team, 8 - availableSlots);
                break;
            case PlayerType.Forward:
                team.GetType().GetProperty("CountA")?.SetValue(team, 6 - availableSlots);
                break;
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
        
        var nominatorId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        
        var teams = new Dictionary<Guid, Team>
        {
            { nominatorId, CreateTeamWithSlots(nominatorId, "Nominator", PlayerType.Goalkeeper, 1) }, // Ha slot
            { otherTeamId, CreateTeamWithSlots(otherTeamId, "Other", PlayerType.Goalkeeper, 0) }      // Non ha slot
        };
        
        var player = CreateSerieAPlayer(1, PlayerType.Goalkeeper);

        // Act
        var result = session.ProcessNomination(nominatorId, player, teams);

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
        
        var nominatorId = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var team3Id = Guid.NewGuid();
        
        var teams = new Dictionary<Guid, Team>
        {
            { nominatorId, CreateTeamWithSlots(nominatorId, "Nominator", PlayerType.Defender, 1) },
            { team2Id, CreateTeamWithSlots(team2Id, "Team2", PlayerType.Defender, 1) },
            { team3Id, CreateTeamWithSlots(team3Id, "Team3", PlayerType.Defender, 0) } // Non ha slot
        };
        
        var player = CreateSerieAPlayer(200, PlayerType.Defender);

        // Act
        var result = session.ProcessNomination(nominatorId, player, teams);

        // Assert
        Assert.False(result.IsAutoAssign);
        Assert.True(result.IsReadyCheck);
        Assert.Equal(PlayerType.Defender, result.Role);
        Assert.NotNull(result.ReadyState);
        Assert.Equal(nominatorId, result.ReadyState.NominatorTeamId);
        Assert.Equal(200, result.ReadyState.SerieAPlayerId);
        Assert.Contains(team2Id, result.ReadyState.EligibleTeamIds); // Solo team2 è eligible
        Assert.DoesNotContain(team3Id, result.ReadyState.EligibleTeamIds); // team3 non ha slot
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
        
        var readyState = session.StartReadyCheck(nominatorId, 400, PlayerType.Forward, eligibleTeams);
        session.ConfirmTeamReady(team2Id); // Completa ready-check

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
        
        var teams = new Dictionary<Guid, Team>
        {
            { Guid.NewGuid(), CreateTeamWithSlots(Guid.NewGuid(), "Team1", PlayerType.Goalkeeper, 1) },
            { Guid.NewGuid(), CreateTeamWithSlots(Guid.NewGuid(), "Team2", PlayerType.Goalkeeper, 1) }
        };
        
        // Simula bidding attivo
        var nominatorId = teams.Keys.First();
        session.StartReadyCheck(nominatorId, 800, PlayerType.Goalkeeper, teams.Keys.Skip(1).ToList());
        session.ConfirmTeamReady(teams.Keys.Skip(1).First());
        session.StartBiddingAfterReady();

        // Act
        var nextTurn = session.FinalizeBidding(teams);

        // Assert
        Assert.NotNull(nextTurn);
        Assert.False(session.IsBiddingActive);
    }
}
