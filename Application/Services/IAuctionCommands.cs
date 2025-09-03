using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.ValueObjects;

namespace Application.Services;

public record CommandResult(bool IsSuccess, string Message = "");

public interface IAuctionCommands
{
    Task NominateAsync(Guid leagueId, Guid nominatorTeamId, int serieAPlayerId, CancellationToken ct = default);
    Task PlaceBidAsync(Guid leagueId, Guid teamId, int amount, CancellationToken ct = default);
    Task<CommandResult> FinalizeTurnAsync(Guid leagueId, CancellationToken ct = default);
    
    // Ready-check management
    Task<bool> ConfirmTeamReadyAsync(Guid sessionId, Guid teamId, CancellationToken ct = default);
    Task<BiddingInfo?> StartBiddingAfterReadyAsync(Guid sessionId, CancellationToken ct = default);
}
