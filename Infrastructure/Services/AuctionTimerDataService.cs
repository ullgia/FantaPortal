using Application.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Infrastructure.Services;

public sealed class AuctionTimerDataService(ApplicationDbContext db) : IAuctionTimerDataService
{
    private readonly ApplicationDbContext _db = db;

    public async Task<PersistedTimer?> GetTimerAsync(Guid turnId, CancellationToken ct = default)
    {
        return await _db.PersistedTimers.FirstOrDefaultAsync(t => t.TurnId == turnId, ct);
    }

    public async Task SaveTimerAsync(PersistedTimer timer, CancellationToken ct = default)
    {
        var existing = await _db.PersistedTimers.FirstOrDefaultAsync(t => t.TurnId == timer.TurnId, ct);
        
        if (existing != null)
        {
            // Update the existing timer - use domain methods instead of setting properties directly
            if (timer.IsPaused && !existing.IsPaused)
            {
                existing.Pause();
            }
            else if (!timer.IsPaused && existing.IsPaused)
            {
                existing.Resume();
            }
            
            // Update remaining seconds using domain method
            var currentRemaining = timer.GetRemainingSeconds();
            if (currentRemaining != existing.GetRemainingSeconds())
            {
                existing.UpdateRemainingSeconds(currentRemaining);
            }
            
            _db.Update(existing);
        }
        else
        {
            _db.PersistedTimers.Add(timer);
        }
        
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteTimerAsync(Guid turnId, CancellationToken ct = default)
    {
        var timer = await _db.PersistedTimers.FirstOrDefaultAsync(t => t.TurnId == turnId, ct);
        if (timer != null)
        {
            _db.PersistedTimers.Remove(timer);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<PersistedTimer>> GetActiveTimersAsync(CancellationToken ct = default)
    {
        return await _db.PersistedTimers
            .Where(t => t.IsActive && !t.IsPaused)
            .ToListAsync(ct);
    }

    public async Task<List<PersistedTimer>> GetTimersForAuctionAsync(Guid auctionId, CancellationToken ct = default)
    {
        return await _db.PersistedTimers
            .Where(t => t.AuctionId == auctionId)
            .ToListAsync(ct);
    }
}
