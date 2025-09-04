using System;
using Xunit;
using Domain.Enums;
using Domain.Entities;
using Domain.Exceptions;

namespace Tests.Unit;

/// <summary>
/// Test di base per validare le mappature e le costanti del sistema
/// </summary>
public class BasicMappingTests
{
    [Fact]
    public void PlayerType_Mapping_Should_Work_Correctly()
    {
        // Test enum PlayerType
        Assert.Equal(PlayerType.Goalkeeper, PlayerType.Goalkeeper);
        Assert.Equal(PlayerType.Defender, PlayerType.Defender);
        Assert.Equal(PlayerType.Midfielder, PlayerType.Midfielder);
        Assert.Equal(PlayerType.Forward, PlayerType.Forward);
        
        // Test che tutti i valori enum siano definiti
        var enumValues = Enum.GetValues<PlayerType>();
        Assert.Equal(4, enumValues.Length);
    }

    [Fact]
    public void SerieAPlayer_Create_Should_Work_With_Valid_Data()
    {
        // Arrange
        var id = 1;
        var role = "P";
        var name = "Test Player";
        var team = "Test Team";
        var qtA = 10.0m;
        var qtI = 8.0m;
        var fvm = 75;

        // Act
        var player = SerieAPlayer.Create(id, role, role, name, team, qtA, qtI, fvm);

        // Assert
        Assert.NotNull(player);
        Assert.Equal(id, player.Id);
        Assert.Equal(role, player.Role);
        Assert.Equal(name, player.Name);
        Assert.Equal(team, player.Team);
        Assert.Equal(qtA, player.QuotationA);
        Assert.Equal(qtI, player.QuotationI);
        Assert.Equal(fvm, player.FVM);
        Assert.Equal(PlayerType.Goalkeeper, player.PlayerType);
    }

    [Theory]
    [InlineData("P", PlayerType.Goalkeeper)]
    [InlineData("D", PlayerType.Defender)]
    [InlineData("C", PlayerType.Midfielder)]
    [InlineData("M", PlayerType.Midfielder)]
    [InlineData("A", PlayerType.Forward)]
    [InlineData("F", PlayerType.Forward)]
    public void SerieAPlayer_PlayerType_Should_Map_Role_Correctly(string role, PlayerType expectedType)
    {
        // Arrange & Act
        var player = SerieAPlayer.Create(1, role, role, "Test", "Test", 10m, 8m, 75);

        // Assert
        Assert.Equal(expectedType, player.PlayerType);
    }

    [Fact]
    public void SerieAPlayer_Create_With_Invalid_Role_Should_Not_Throw_On_Creation()
    {
        // Arrange & Act - La creazione non dovrebbe fallire
        var player = SerieAPlayer.Create(1, "X", "X", "Test", "Test", 10m, 8m, 75);

        // Assert - Ma l'accesso a PlayerType dovrebbe fallire
        Assert.NotNull(player);
        Assert.Throws<DomainException>(() => player.PlayerType);
    }
}
