using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Entities;
using Domain.Enums;
using Domain.Services;
using Moq;
using Xunit;

namespace Tests;

public class AuctionFlowTests
{
    private async Task<Team> MakeTeam(string name, int budget, int p, int d, int c, int a)
    {
        var validator = new Mock<ITeamValidator>();
        validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var t = await Team.CreateAsync(Guid.NewGuid(), name, budget, validator.Object);
        // Fill counts with Assign (simulate existing roster)
        while (t.CountP < p) t.Assign(RoleType.P, 1);
        while (t.CountD < d) t.Assign(RoleType.D, 1);
        while (t.CountC < c) t.Assign(RoleType.C, 1);
        while (t.CountA < a) t.Assign(RoleType.A, 1);
        return t;
    }

    [Fact]
    public async Task AutoAssign_When_No_Other_Eligible()
    {
        var A = await MakeTeam("A", 100, 0, 0, 0, 0);
        var B = await MakeTeam("B", 100, 3, 0, 0, 0); // full P
        var C = await MakeTeam("C", 100, 3, 0, 0, 0); // full P
        var teams = new[] { A, B, C };
        var res = AuctionFlow.EvaluateNomination(teams, A.Id, RoleType.P);
        Assert.True(res.AutoAssign);
        Assert.Equal(1, res.AssignPrice);
        Assert.Empty(res.EligibleOthers);
    }

    [Fact]
    public async Task NextNominator_Cycles_Until_Eligible()
    {
        var A = await MakeTeam("A", 100, 0, 0, 0, 0); // has slot P
        var B = await MakeTeam("B", 100, 3, 0, 0, 0); // full P
        var C = await MakeTeam("C", 100, 3, 0, 0, 0); // full P
        var order = new List<Guid> { A.Id, B.Id, C.Id };
        var map = new Dictionary<Guid, Team> { { A.Id, A }, { B.Id, B }, { C.Id, C } };

        var idx = AuctionFlow.FindNextEligibleIndex(order, 0, map, RoleType.P);
        Assert.Equal(0, idx); // A is eligible

        // After A nominates and is auto-assigned, still A should be next (because others have no slot)
        idx = AuctionFlow.FindNextEligibleIndex(order, (idx + 1) % order.Count, map, RoleType.P);
        Assert.Equal(0, idx);
    }

    [Fact]
    public async Task Advance_Role_When_All_Full_For_Current_Role()
    {
        var A = await MakeTeam("A", 100, 3, 0, 0, 0);
        var B = await MakeTeam("B", 100, 3, 0, 0, 0);
        var C = await MakeTeam("C", 100, 3, 0, 0, 0);
        var order = new List<Guid> { A.Id, B.Id, C.Id };
        var map = new Dictionary<Guid, Team> { { A.Id, A }, { B.Id, B }, { C.Id, C } };

        var (role, idx) = AuctionFlow.AdvanceUntilEligible(order, map, RoleType.P, 0);
        Assert.Equal(RoleType.D, role); // move to defenders
        Assert.InRange(idx, 0, order.Count - 1);
    }
}
