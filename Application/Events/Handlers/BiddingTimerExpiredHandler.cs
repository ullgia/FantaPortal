using Application.Services;
using Microsoft.Extensions.Logging;

namespace Application.Events.Handlers;

public class BiddingTimerExpiredHandler : IDomainEventHandler<BiddingTimerExpired>
{
    private readonly IAuctionCommands _auctionCommands;
    private readonly ILogger<BiddingTimerExpiredHandler> _logger;

    public BiddingTimerExpiredHandler(IAuctionCommands auctionCommands, ILogger<BiddingTimerExpiredHandler> logger)
    {
        _auctionCommands = auctionCommands;
        _logger = logger;
    }

    public async Task Handle(BiddingTimerExpired @event)
    {
        try
        {
            _logger.LogInformation("Bidding timer expired for turn {TurnId}, finalizing league {LeagueId}", 
                @event.TurnId, @event.LeagueId);
                
            // Finalize the turn using League-based approach
            var result = await _auctionCommands.FinalizeTurnAsync(@event.LeagueId);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully finalized turn {TurnId} for league {LeagueId}: {Message}", 
                    @event.TurnId, @event.LeagueId, result.Message);
            }
            else
            {
                _logger.LogWarning("Failed to finalize turn {TurnId} for league {LeagueId}: {Message}", 
                    @event.TurnId, @event.LeagueId, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling bidding timer expired for turn {TurnId}, league {LeagueId}", 
                @event.TurnId, @event.LeagueId);
        }
    }
}
