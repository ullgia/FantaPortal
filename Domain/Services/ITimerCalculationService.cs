namespace Domain.Services;

using Domain.Entities;
using Domain.Enums;

/// <summary>
/// Service responsible for computing the timer behavior when a bid is accepted.
/// Implementations may use additive, bucketed or adaptive strategies.
/// </summary>
public interface ITimerCalculationService
{
    /// <summary>
    /// Calculate the remaining seconds that should be set on the turn after a bid is accepted.
    /// The implementation can inspect the auction and the current turn (including bids) to decide.
    /// </summary>
    /// <param name="auction">The auction event settings</param>
    /// <param name="turn">The current auction turn containing bids and state</param>
    /// <returns>New remaining seconds to persist (non-negative)</returns>
    int CalculateNewRemainingSeconds(AuctionSession auction, AuctionTurn turn);


    TimerCalculationStrategy SupportedStrategy { get; }
}


public sealed record BiddingContext(
    int BaseTimerSeconds,
    IReadOnlyList<BidSnapshot> Bids,
    DateTime NowUtc);

public sealed record BidSnapshot(Guid TeamId, int Amount, DateTime TimestampUtc);

public interface ITimerCalculationServiceFactory
{
    ITimerCalculationService Resolve(TimerCalculationStrategy strategy);
}
