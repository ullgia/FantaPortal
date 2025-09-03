using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Contracts;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Events;
using Moq;
using Xunit;

namespace Tests;

public class TeamTests
{
    [Fact]
    public async Task CreateAsync_Should_Create_Team_When_Name_Unique()
    {
        var validator = new Mock<ITeamValidator>();
        validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var leagueId = Guid.NewGuid();
        var team = await Team.CreateAsync(leagueId, "Juventus", 500, validator.Object);

        Assert.Equal(leagueId, team.LeagueId);
        Assert.Equal("Juventus", team.Name);
        Assert.Equal(500, team.Budget);
    }

    [Fact]
    public async Task CreateAsync_Should_Raise_TeamCreated_DomainEvent()
    {
        var validator = new Mock<ITeamValidator>();
        validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var team = await Team.CreateAsync(Guid.NewGuid(), "Juventus", 500, validator.Object);

        var evt = team.DomainEvents.OfType<TeamCreated>().SingleOrDefault();
        Assert.NotNull(evt);
        Assert.Equal(team.Id, evt!.TeamId);
        Assert.Equal(team.LeagueId, evt.LeagueId);
        Assert.Equal(team.Name, evt.Name);
    }

    [Fact]
    public async Task CreateAsync_Should_Throw_When_Name_Not_Unique()
    {
        var validator = new Mock<ITeamValidator>();
        validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        await Assert.ThrowsAsync<DomainException>(async () =>
            await Team.CreateAsync(Guid.NewGuid(), "Milan", 400, validator.Object));
    }

    [Fact]
    public async Task CreateAsync_Should_Normalize_Name_And_Check_Case_Insensitive()
    {
        var validator = new Mock<ITeamValidator>();
        validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), "juventus", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var team = await Team.CreateAsync(Guid.NewGuid(), "  Juventus  ", 100, validator.Object);
        Assert.Equal("Juventus", team.Name);
    }

    [Fact]
    public async Task Assign_Should_Decrease_Budget_And_Increase_Slot()
    {
        var validator = new Mock<ITeamValidator>();
        validator.Setup(v => v.IsTeamNameUniqueAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var team = await Team.CreateAsync(Guid.NewGuid(), "TestTeam", 100, validator.Object);

        team.Assign(RoleType.P, 20);

        Assert.Equal(80, team.Budget);
        Assert.Equal(1, team.CountP);
    }
}
