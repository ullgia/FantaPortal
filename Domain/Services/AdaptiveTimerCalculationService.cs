namespace Domain.Services;

using Domain.Entities;
using Domain.Enums;

/// <summary>
/// Strategia adattiva: calcola una estensione basata sulla media dell'intervallo tra le ultime offerte.
/// Clampa tra 5 secondi e il timer base.
/// </summary>
public sealed class AdaptiveTimerCalculationService : ITimerCalculationService
{
    public TimerCalculationStrategy SupportedStrategy => TimerCalculationStrategy.Adaptive;

    public int CalculateNewRemainingSeconds(AuctionSession auction, AuctionTurn turn)
    {
        var bids = (turn.Bids ?? Array.Empty<Bid>()).ToList();
        if (bids.Count < 2)
            return Math.Min(auction.EffectiveTimerSeconds, 30);

        // compute intervals in seconds
        var intervals = new List<double>();
        for (int i = 1; i < bids.Count; i++)
        {
            var prev = bids[i - 1].Timestamp;
            var cur = bids[i].Timestamp;
            intervals.Add((cur - prev).TotalSeconds);
        }

        var avg = intervals.Average();
        // Map average interval to extension: clamp between 5 and auction.EffectiveTimerSeconds
        var extension = (int)Math.Round(Math.Clamp(avg, 5, auction.EffectiveTimerSeconds));
        return extension;
    }
}
