using System;
using Domain.Entities;
using Domain.Exceptions;
using Xunit;

namespace Tests;

public class AuctionSessionTests
{
    [Fact]
    public void Create_Should_Set_Defaults_And_Allow_State_Transitions()
    {
        var leagueId = Guid.NewGuid();
        var session = AuctionSession.Create(leagueId);
        Assert.Equal(leagueId, session.LeagueId);
        session.Start();
        session.Pause();
        session.ReviewPhase();
        session.Complete();
        // If no exception thrown, transitions are allowed
    }

    [Fact]
    public void Create_Should_Throw_When_LeagueId_Empty()
    {
        Assert.Throws<DomainException>(() => AuctionSession.Create(Guid.Empty));
    }
}
