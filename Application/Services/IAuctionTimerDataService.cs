using Domain.Entities;

namespace Application.Services;

public interface IAuctionTimerDataService
{
    Task<PersistedTimer?> GetTimerAsync(Guid turnId, CancellationToken ct = default);
    Task SaveTimerAsync(PersistedTimer timer, CancellationToken ct = default);
    Task DeleteTimerAsync(Guid turnId, CancellationToken ct = default);
    Task<List<PersistedTimer>> GetActiveTimersAsync(CancellationToken ct = default);
    Task<List<PersistedTimer>> GetTimersForAuctionAsync(Guid auctionId, CancellationToken ct = default);
}
