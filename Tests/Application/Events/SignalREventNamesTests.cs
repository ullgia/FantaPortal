using Application.Events;
using System;
using System.Linq;
using Xunit;

namespace Tests.Application.Events;

public class SignalREventNamesTests
{
    [Fact]
    public void SignalREventNames_Should_HaveUniqueValues()
    {
        // Arrange
        var eventNames = new[]
        {
            SignalREventNames.AuctionStateChanged,
            SignalREventNames.BiddingTimerUpdate,
            SignalREventNames.NewHighestBid,
            SignalREventNames.ReadyCheckStarted,
            SignalREventNames.BiddingStarted,
            SignalREventNames.TurnAdvanced,
            SignalREventNames.TurnOrderUpdate,
            SignalREventNames.PlayerNominated,
            SignalREventNames.ReadyStatesUpdate,
            SignalREventNames.BidUpdate,
            SignalREventNames.FullStateUpdate,
            SignalREventNames.PhaseChanged,
            SignalREventNames.ConnectionEstablished,
            SignalREventNames.ConnectionLost,
            SignalREventNames.ErrorOccurred,
            SignalREventNames.UserJoinedGroup,
            SignalREventNames.UserLeftGroup,
            SignalREventNames.GroupUpdate
        };

        // Act & Assert
        Assert.Equal(eventNames.Length, eventNames.Distinct().Count());
    }

    [Theory]
    [InlineData(nameof(SignalREventNames.AuctionStateChanged), "AuctionStateChanged")]
    [InlineData(nameof(SignalREventNames.BiddingTimerUpdate), "BiddingTimerUpdate")]
    [InlineData(nameof(SignalREventNames.NewHighestBid), "NewHighestBid")]
    [InlineData(nameof(SignalREventNames.ReadyCheckStarted), "ReadyCheckStarted")]
    [InlineData(nameof(SignalREventNames.BiddingStarted), "BiddingStarted")]
    [InlineData(nameof(SignalREventNames.TurnAdvanced), "TurnAdvanced")]
    [InlineData(nameof(SignalREventNames.TurnOrderUpdate), "TurnOrderUpdate")]
    [InlineData(nameof(SignalREventNames.PlayerNominated), "PlayerNominated")]
    [InlineData(nameof(SignalREventNames.ReadyStatesUpdate), "ReadyStatesUpdate")]
    [InlineData(nameof(SignalREventNames.BidUpdate), "BidUpdate")]
    [InlineData(nameof(SignalREventNames.FullStateUpdate), "FullStateUpdate")]
    [InlineData(nameof(SignalREventNames.PhaseChanged), "PhaseChanged")]
    public void SignalREventNames_Should_HaveCorrectValues(string propertyName, string expectedValue)
    {
        // Arrange & Act
        var actualValue = typeof(SignalREventNames)
            .GetField(propertyName)?
            .GetValue(null) as string;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void SignalREventNames_Should_NotBeEmptyOrNull()
    {
        // Arrange
        var fields = typeof(SignalREventNames).GetFields();

        // Act & Assert
        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            Assert.False(string.IsNullOrEmpty(value), $"Event name {field.Name} should not be null or empty");
        }
    }

    [Fact]
    public void SignalREventNames_Should_FollowNamingConvention()
    {
        // Arrange
        var fields = typeof(SignalREventNames).GetFields();

        // Act & Assert
        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            
            // Should be PascalCase
            Assert.True(char.IsUpper(value![0]), $"Event name {value} should start with uppercase");
            
            // Should not contain spaces or special characters (except maybe numbers)
            Assert.True(value.All(c => char.IsLetterOrDigit(c)), 
                $"Event name {value} should only contain letters and digits");
        }
    }
}
