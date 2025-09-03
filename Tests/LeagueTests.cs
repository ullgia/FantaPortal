using Domain.Entities;
using Domain.Exceptions;
using Xunit;

namespace Tests;

public class LeagueTests
{
    [Fact]
    public void Create_Should_Create_League_With_Trimmed_Name()
    {
        var league = League.Create("  Serie A  ");
        Assert.Equal("Serie A", league.Name);
    }

    [Fact]
    public void Create_Should_Throw_When_Name_Invalid()
    {
        Assert.Throws<DomainException>(() => League.Create(" "));
    }
}
