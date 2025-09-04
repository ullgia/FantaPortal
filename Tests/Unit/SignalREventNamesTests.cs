using System.Linq;
using Xunit;
using Application.Events;

namespace Tests.Unit;

/// <summary>
/// Test per verificare che le costanti SignalR siano definite correttamente
/// e non cambino accidentalmente, causando problemi di comunicazione.
/// </summary>
public class SignalREventNamesTests
{
    [Fact]
    public void SignalREventNames_Should_Have_Correct_Values()
    {
        // Test eventi legacy
        Assert.Equal("AuctionStateChanged", SignalREventNames.AuctionStateChanged);
        Assert.Equal("BiddingTimerUpdate", SignalREventNames.BiddingTimerUpdate);
        Assert.Equal("NewHighestBid", SignalREventNames.NewHighestBid);
        Assert.Equal("ReadyCheckStarted", SignalREventNames.ReadyCheckStarted);
        Assert.Equal("BiddingStarted", SignalREventNames.BiddingStarted);
        Assert.Equal("TurnAdvanced", SignalREventNames.TurnAdvanced);
        
        // Test eventi ottimizzati
        Assert.Equal("TurnOrderUpdate", SignalREventNames.TurnOrderUpdate);
        Assert.Equal("PlayerNominated", SignalREventNames.PlayerNominated);
        Assert.Equal("ReadyStatesUpdate", SignalREventNames.ReadyStatesUpdate);
        Assert.Equal("BidUpdate", SignalREventNames.BidUpdate);
        Assert.Equal("FullStateUpdate", SignalREventNames.FullStateUpdate);
        Assert.Equal("PhaseChanged", SignalREventNames.PhaseChanged);
        
        // Test eventi di sistema
        Assert.Equal("ConnectionEstablished", SignalREventNames.ConnectionEstablished);
        Assert.Equal("ConnectionLost", SignalREventNames.ConnectionLost);
        Assert.Equal("ErrorOccurred", SignalREventNames.ErrorOccurred);
    }

    [Fact]
    public void SignalREventNames_Should_Not_Be_Null_Or_Empty()
    {
        // Test che tutti i valori siano definiti e non vuoti
        var eventNamesType = typeof(SignalREventNames);
        var constants = eventNamesType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));

        foreach (var constant in constants)
        {
            var value = constant.GetValue(null) as string;
            Assert.False(string.IsNullOrEmpty(value), $"Event name {constant.Name} should not be null or empty");
        }
    }

    [Fact]
    public void SignalREventNames_Should_Not_Have_Duplicates()
    {
        // Test che non ci siano valori duplicati
        var eventNamesType = typeof(SignalREventNames);
        var constants = eventNamesType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));

        var values = constants.Select(c => c.GetValue(null) as string).ToList();
        var distinctValues = values.Distinct().ToList();

        Assert.Equal(values.Count, distinctValues.Count);
    }
}
