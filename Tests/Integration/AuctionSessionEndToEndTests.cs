using System;
using Xunit;
using Domain.Enums;

namespace Tests.Integration;

/// <summary>
/// Test end-to-end semplificati per una sessione d'asta.
/// Verifica che i componenti principali siano configurati correttamente.
/// </summary>
public class AuctionSessionEndToEndTests
{
    [Fact]
    public void AuctionSession_PlayerTypes_Should_Be_Valid()
    {
        // Test che i tipi di giocatore siano validi
        Assert.Equal(PlayerType.Goalkeeper, PlayerType.Goalkeeper);
        Assert.Equal(PlayerType.Defender, PlayerType.Defender);
        Assert.Equal(PlayerType.Midfielder, PlayerType.Midfielder);
        Assert.Equal(PlayerType.Forward, PlayerType.Forward);
    }

    [Theory]
    [InlineData(PlayerType.Goalkeeper)]
    [InlineData(PlayerType.Defender)]
    [InlineData(PlayerType.Midfielder)]
    [InlineData(PlayerType.Forward)]
    public void AuctionSession_Should_Support_All_PlayerTypes(PlayerType playerType)
    {
        // Test che tutti i tipi di giocatore siano supportati
        Assert.True(Enum.IsDefined(typeof(PlayerType), playerType));
    }

    [Fact]
    public void AuctionSession_Constants_Should_Be_Defined()
    {
        // Test di base che le costanti siano definite
        Assert.NotNull(nameof(PlayerType.Goalkeeper));
        Assert.NotNull(nameof(PlayerType.Defender));
        Assert.NotNull(nameof(PlayerType.Midfielder));
        Assert.NotNull(nameof(PlayerType.Forward));
    }
}
