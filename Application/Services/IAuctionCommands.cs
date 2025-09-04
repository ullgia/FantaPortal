using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.Services;

public record CommandResult(bool IsSuccess, string Message = "");

public interface IAuctionCommands
{
    // Auction lifecycle operations
    Task<CommandResult> StartAuctionAsync(Guid leagueId, CancellationToken ct = default);
    Task<CommandResult> PauseAuctionAsync(Guid leagueId, CancellationToken ct = default);
    Task<CommandResult> ResumeAuctionAsync(Guid leagueId, CancellationToken ct = default);
    Task<CommandResult> CompleteAuctionAsync(Guid leagueId, CancellationToken ct = default);
    
    // Player nomination and bidding
    Task NominatePlayerAsync(Guid auctionId, Guid teamId, int playerId, CancellationToken ct = default);
    Task PlaceBidAsync(Guid auctionId, Guid teamId, int amount, ITimerCalculationServiceFactory factory, CancellationToken ct = default);
    Task<CommandResult> FinalizeTurnAsync(Guid leagueId, CancellationToken ct = default);
    
    // Ready-check management
    Task<bool> ConfirmTeamReadyAsync(Guid sessionId, Guid teamId, CancellationToken ct = default);
    Task<BiddingInfo?> StartBiddingAfterReadyAsync(Guid sessionId, CancellationToken ct = default);
    
    // Advanced auction operations
    Task<CommandResult> ForceTurnAdvancementAsync(Guid leagueId, CancellationToken ct = default);
    Task<CommandResult> UndoLastAssignmentAsync(Guid leagueId, CancellationToken ct = default);
    Task<CommandResult> UpdateAuctionOrderAsync(Guid leagueId, IReadOnlyList<Guid> newOrder, CancellationToken ct = default);
}
