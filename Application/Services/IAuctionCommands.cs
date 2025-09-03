using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services;

public record CommandResult(bool IsSuccess, string Message = "");

public interface IAuctionCommands
{
    Task NominateAsync(Guid sessionId, Guid nominatorTeamId, int serieAPlayerId, CancellationToken ct = default);
    Task MarkReadyAsync(Guid sessionId, Guid teamId, CancellationToken ct = default);
    Task PlaceBidAsync(Guid sessionId, Guid teamId, int amount, CancellationToken ct = default);
    Task<CommandResult> FinalizeTurnAsync(Guid turnId, CancellationToken ct = default);
}
