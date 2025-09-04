using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Domain.Services;
using Domain.ValueObjects;
using Infrastructure.Peristance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AuctionCommands(ApplicationDbContext db) : IAuctionCommands
{
    private readonly ApplicationDbContext _db = db;

    
    public async Task<CommandResult> FinalizeTurnAsync(Guid leagueId, CancellationToken ct = default)
    {
        try
        {
            // Load League with all related entities
            var league = await _db.Leagues
                .Include(l => l.Teams)
                .Include(l => l.PlayerOwnerships) 
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            if (league.ActiveAuction?.IsBiddingActive != true)
            {
                return new CommandResult(false, "No active bidding to finalize");
            }

            // Need to get the current nominated player to finalize
            var currentPlayerId = league.ActiveAuction.CurrentSerieAPlayerId;
            var serieAPlayer = await _db.SerieAPlayers.FirstOrDefaultAsync(p => p.Id == currentPlayerId, ct);
            
            if (serieAPlayer == null)
            {
                return new CommandResult(false, "Current nominated player not found");
            }

            // League orchestrates the finalization
            league.FinalizeBiddingRound(serieAPlayer);

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Bidding round finalized successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error finalizing turn: {ex.Message}");
        }
    }

    public async Task<bool> ConfirmTeamReadyAsync(Guid sessionId, Guid teamId, CancellationToken ct = default)
    {
        try
        {
            // Troviamo la League che contiene questa sessione
            var league = await _db.Leagues
                .Include(l => l.ActiveAuction)
                    .ThenInclude(s => s!.ReadyStates)
                .FirstOrDefaultAsync(l => l.ActiveAuction != null && l.ActiveAuction.Id == sessionId, ct);

            if (league?.ActiveAuction == null) return false;

            // Usa League per confermare ready (metodo pubblico)
            var confirmed = league.ConfirmTeamReady(teamId);
            
            if (confirmed)
            {
                _db.Update(league);
                await _db.SaveChangesAsync(ct);
            }

            return confirmed;
        }
        catch
        {
            return false;
        }
    }

    public async Task NominatePlayerAsync(Guid auctionId, Guid teamId, int playerId, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.ActiveAuction)
                .Include(l => l.Teams)
                .FirstOrDefaultAsync(l => l.ActiveAuction != null && l.ActiveAuction.Id == auctionId, ct);

            if (league?.ActiveAuction == null)
                throw new InvalidOperationException("Auction not found");

            var player = await _db.SerieAPlayers.FindAsync(playerId);
            if (player == null)
                throw new InvalidOperationException("Player not found");

            league.NominatePlayer(teamId, player);
            
            _db.Update(league);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task PlaceBidAsync(Guid auctionId, Guid teamId, int amount,ITimerCalculationServiceFactory factory, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.ActiveAuction)
                .Include(l => l.Teams)
                .FirstOrDefaultAsync(l => l.ActiveAuction != null && l.ActiveAuction.Id == auctionId, ct);

            if (league?.ActiveAuction == null)
                throw new InvalidOperationException("Auction not found");

            league.PlaceBid(teamId, amount, factory);
            
            _db.Update(league);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BiddingInfo?> StartBiddingAfterReadyAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            // Troviamo la League che contiene questa sessione
            var league = await _db.Leagues
                .Include(l => l.ActiveAuction)
                    .ThenInclude(s => s!.ReadyStates)
                .FirstOrDefaultAsync(l => l.ActiveAuction != null && l.ActiveAuction.Id == sessionId, ct);

            if (league?.ActiveAuction == null) return null;

            // Usa League per avviare bidding dopo ready (metodo pubblico)
            var biddingInfo = league.StartBiddingAfterReady();
            
            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return biddingInfo;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CommandResult> StartAuctionAsync(Guid leagueId, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.Teams)
                .Include(l => l.PlayerOwnerships)
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            league.StartAuction();

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Auction started successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error starting auction: {ex.Message}");
        }
    }

    public async Task<CommandResult> PauseAuctionAsync(Guid leagueId, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            league.PauseAuction();

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Auction paused successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error pausing auction: {ex.Message}");
        }
    }

    public async Task<CommandResult> ResumeAuctionAsync(Guid leagueId, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            league.ResumeAuction();

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Auction resumed successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error resuming auction: {ex.Message}");
        }
    }

    public async Task<CommandResult> CompleteAuctionAsync(Guid leagueId, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.Teams)
                .Include(l => l.PlayerOwnerships)
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            league.CompleteAuction();

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Auction completed successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error completing auction: {ex.Message}");
        }
    }

    public async Task<CommandResult> ForceTurnAdvancementAsync(Guid leagueId, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.Teams)
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            league.ForceTurnAdvancement();

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Turn advancement forced successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error forcing turn advancement: {ex.Message}");
        }
    }

    public async Task<CommandResult> UndoLastAssignmentAsync(Guid leagueId, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.Teams)
                .Include(l => l.PlayerOwnerships)
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            league.UndoLastAssignment();

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Last assignment undone successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error undoing last assignment: {ex.Message}");
        }
    }

    public async Task<CommandResult> UpdateAuctionOrderAsync(Guid leagueId, IReadOnlyList<Guid> newOrder, CancellationToken ct = default)
    {
        try
        {
            var league = await _db.Leagues
                .Include(l => l.Teams)
                .Include(l => l.ActiveAuction)
                .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
                ?? throw new InvalidOperationException("League not found");

            league.UpdateAuctionOrder(newOrder);

            _db.Update(league);
            await _db.SaveChangesAsync(ct);

            return new CommandResult(true, "Auction order updated successfully");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"Error updating auction order: {ex.Message}");
        }
    }
}
