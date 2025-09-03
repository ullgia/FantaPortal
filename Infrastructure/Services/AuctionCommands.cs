using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Infrastructure.Services;

public sealed class AuctionCommands(ApplicationDbContext db) : IAuctionCommands
{
    private readonly ApplicationDbContext _db = db;

    public async Task NominateAsync(Guid leagueId, Guid nominatorTeamId, int serieAPlayerId, CancellationToken ct = default)
    {
        // Load League with all related entities (teams, ownerships, active auction)
        var league = await _db.Leagues
            .Include(l => l.Teams)
            .Include(l => l.PlayerOwnerships)
            .Include(l => l.ActiveAuction)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct) 
            ?? throw new InvalidOperationException("League not found");

        var serieAPlayer = await _db.SerieAPlayers.FirstOrDefaultAsync(p => p.Id == serieAPlayerId, ct) 
            ?? throw new InvalidOperationException("Player not found");

        // League orchestrates the nomination
        league.NominatePlayer(nominatorTeamId, serieAPlayer);

        _db.Update(league);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PlaceBidAsync(Guid leagueId, Guid teamId, int amount, CancellationToken ct = default)
    {
        // Load League with all related entities  
        var league = await _db.Leagues
            .Include(l => l.Teams)
            .Include(l => l.PlayerOwnerships)
            .Include(l => l.ActiveAuction)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct)
            ?? throw new InvalidOperationException("League not found");

        // League orchestrates the bid
        league.PlaceBid(teamId, amount);

        _db.Update(league);
        await _db.SaveChangesAsync(ct);
    }

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
                    .ThenInclude(s => s.ReadyStates)
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

    public async Task<BiddingInfo?> StartBiddingAfterReadyAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            // Troviamo la League che contiene questa sessione
            var league = await _db.Leagues
                .Include(l => l.ActiveAuction)
                    .ThenInclude(s => s.ReadyStates)
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
}
