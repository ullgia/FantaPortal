using System;
using System.Collections.Generic;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Tests.Domain;

public class BiddingReadyStateDebugTests
{
    [Fact]
    public void DebugSimpleReadyCheck()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var nominatorId = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var eligibleTeams = new List<Guid> { team2Id };
        
        var readyState = BiddingReadyState.Create(sessionId, nominatorId, 300, PlayerType.Midfielder, eligibleTeams);

        // Debug initial state
        Assert.False(readyState.AllTeamsReady);
        Assert.False(readyState.IsCompleted);
        Assert.Single(readyState.EligibleTeamIds);
        Assert.Equal(team2Id, readyState.EligibleTeamIds[0]);

        // Act - Mark team2 as ready
        var markResult = readyState.MarkTeamReady(team2Id);
        
        // Debug after marking ready
        Assert.True(markResult);
        Assert.True(readyState.AllTeamsReady);
        Assert.False(readyState.IsCompleted); // Should still be false until Complete() is called
        
        // Complete manually
        readyState.Complete();
        
        // Debug final state
        Assert.True(readyState.AllTeamsReady);
        Assert.True(readyState.IsCompleted);
    }
    
    [Fact]
    public void DebugAuctionSessionReadyCheck()
    {
        // Arrange
        var session = AuctionSession.CreateInternal(Guid.NewGuid(), 1, 1, new List<Guid>());
        session.Start();
        
        var nominatorId = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var eligibleTeams = new List<Guid> { team2Id };
        
        var readyState = session.StartReadyCheck(nominatorId, 300, PlayerType.Midfielder, eligibleTeams);

        // Debug initial state
        Assert.False(readyState.AllTeamsReady);
        Assert.False(readyState.IsCompleted);

        // Act - Team2 conferma ready
        var confirmed = session.ConfirmTeamReady(team2Id);

        // Debug final state
        Assert.True(confirmed, "ConfirmTeamReady should return true");
        Assert.True(readyState.AllTeamsReady, "AllTeamsReady should be true after all teams confirmed");
        Assert.True(readyState.IsCompleted, "IsCompleted should be true after ConfirmTeamReady calls Complete()");
    }
}
