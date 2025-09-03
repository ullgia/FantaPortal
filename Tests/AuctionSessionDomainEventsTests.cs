using System;
using System.Linq;
using Xunit;
using Domain.Entities;
using Domain.Events;

namespace Tests;

public class AuctionSessionDomainEventsTests
{
    [Fact]
    public void Start_Should_Raise_AuctionSessionStarted_Event()
    {
        var session = AuctionSession.Create(Guid.NewGuid());
        session.Start();
        var evt = session.DomainEvents.OfType<AuctionSessionStarted>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(session.Id, evt!.SessionId);
        Assert.Equal(session.LeagueId, evt.LeagueId);
    }
}
