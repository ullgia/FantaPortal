using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Portal.Data;

namespace Infrastructure.Services;

public sealed class AuctionQueries(ApplicationDbContext db) : IAuctionQueries
{
    private readonly ApplicationDbContext _db = db;

    public async Task<AuctionStateDto?> GetCurrentAuctionStateAsync(Guid leagueId, CancellationToken ct = default)
    {
        var league = await _db.Leagues
            .Include(l => l.ActiveAuction)
                .ThenInclude(a => a!.ReadyStates)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct);

        if (league?.ActiveAuction == null)
            return null;

        var auction = league.ActiveAuction;

        // Ottieni il team del turno corrente dalla lista ordinata
        var currentTurnTeamId = auction.TeamOrder.ElementAtOrDefault(auction.CurrentOrderIndex);

        // Per le bid correnti, dato che sono gestite tramite BiddingState interno,
        // restituisco una lista vuota per ora - sarà l'applicazione a gestire lo stato
        var currentBids = new List<BidDto>();

        // Recupero i team per i nomi nei ready states
        var teamIds = auction.ReadyStates.SelectMany(rs => rs.EligibleTeamIds).ToList();
        var teams = await _db.Teams
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        return new AuctionStateDto(
            Id: auction.Id,
            Status: auction.Status,
            CurrentRole: auction.CurrentRole,
            CurrentTurnTeamId: currentTurnTeamId,
            CurrentSerieAPlayerId: auction.CurrentSerieAPlayerId != 0 ? auction.CurrentSerieAPlayerId : null,
            IsBiddingActive: auction.IsBiddingActive,
            IsReadyCheckActive: auction.CurrentReadyState != null && !auction.CurrentReadyState.IsCompleted,
            BasePrice: auction.BasePrice,
            MinIncrement: auction.MinIncrement,
            CurrentBids: currentBids,
            ReadyStates: auction.ReadyStates.SelectMany(rs => 
                rs.EligibleTeamIds.Select(teamId => new ReadyStateDto(
                    TeamId: teamId,
                    TeamName: teams.GetValueOrDefault(teamId, "Unknown"),
                    IsReady: rs.ReadyTeamIds.Contains(teamId)
                ))
            ).ToList()
        );
    }

    public async Task<IReadOnlyList<TeamSummaryDto>> GetTeamsSummaryAsync(Guid leagueId, CancellationToken ct = default)
    {
        var teams = await _db.Teams
            .Where(t => t.LeagueId == leagueId)
            .ToListAsync(ct);

        // Per ora usiamo i contatori della Team entity invece di navigare PlayerOwnerships
        // dato che la struttura del database potrebbe non avere questa navigazione configurata
        return teams.Select(team => new TeamSummaryDto(
            Id: team.Id,
            Name: team.Name,
            AvailableBudget: team.Budget,
            SpentBudget: 0, // Non abbiamo InitialBudget nella entity
            PlayersCount: team.CountP + team.CountD + team.CountC + team.CountA,
            RoleSlots: new List<RoleSlotsSummaryDto>
            {
                new(PlayerType.Goalkeeper, team.CountP, team.MaxP),
                new(PlayerType.Defender, team.CountD, team.MaxD),
                new(PlayerType.Midfielder, team.CountC, team.MaxC),
                new(PlayerType.Forward, team.CountA, team.MaxA)
            }
        )).ToList();
    }

    public async Task<LeagueStatsDto?> GetLeagueStatsAsync(Guid leagueId, CancellationToken ct = default)
    {
        var league = await _db.Leagues
            .Include(l => l.Teams)
            .Include(l => l.ActiveAuction)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct);

        if (league == null)
            return null;

        var teams = league.Teams.Select(team => new TeamSummaryDto(
            Id: team.Id,
            Name: team.Name,
            AvailableBudget: team.Budget,
            SpentBudget: 0, // Non abbiamo InitialBudget nella entity
            PlayersCount: team.CountP + team.CountD + team.CountC + team.CountA,
            RoleSlots: new List<RoleSlotsSummaryDto>
            {
                new(PlayerType.Goalkeeper, team.CountP, team.MaxP),
                new(PlayerType.Defender, team.CountD, team.MaxD),
                new(PlayerType.Midfielder, team.CountC, team.MaxC),
                new(PlayerType.Forward, team.CountA, team.MaxA)
            }
        )).ToList();

        var totalBudget = league.Teams.Sum(t => t.Budget); // Budget corrente
        var spentBudget = 0; // Non calcolabile senza InitialBudget
        var playersAssigned = league.Teams.Sum(t => t.CountP + t.CountD + t.CountC + t.CountA);

        return new LeagueStatsDto(
            Id: league.Id,
            Name: league.Name,
            TotalBudget: totalBudget,
            SpentBudget: spentBudget,
            PlayersAssigned: playersAssigned,
            TotalPlayers: 25 * league.Teams.Count, // 25 giocatori per team stimati
            AuctionStatus: league.ActiveAuction?.Status,
            Teams: teams
        );
    }

    public async Task<IReadOnlyList<BidDto>> GetCurrentBidsAsync(Guid leagueId, CancellationToken ct = default)
    {
        var auction = await _db.AuctionSessions
            .FirstOrDefaultAsync(a => a.LeagueId == leagueId && a.Status == AuctionStatus.Running, ct);

        if (auction == null || !auction.IsBiddingActive)
            return new List<BidDto>();

        // Se c'è un bidding attivo, recupero le bid dalla tabella Bid per i turn attivi
        // Le bid sono associate ai turn, quindi cerco le bid per i turn in bidding
        var currentTurnBids = await _db.Bids
            .Where(b => _db.AuctionTurns
                .Where(at => at.SessionId == auction.Id && 
                           at.Status == AuctionTurnStatus.Bidding)
                .Select(at => at.Id)
                .Contains(b.TurnId))
            .OrderByDescending(b => b.Amount)
            .ThenBy(b => b.PlacedAt)
            .ToListAsync(ct);

        // Recupero i nomi dei team per le bid
        var teamIds = currentTurnBids.Select(b => b.TeamId).Distinct().ToList();
        var teams = await _db.Teams
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        return currentTurnBids.Select(bid => new BidDto(
            TeamId: bid.TeamId,
            TeamName: teams.GetValueOrDefault(bid.TeamId, "Unknown"),
            Amount: bid.Amount,
            PlacedAt: bid.PlacedAt
        )).ToList();
    }

    public async Task<IReadOnlyList<ReadyStateDto>> GetReadyStatesAsync(Guid leagueId, CancellationToken ct = default)
    {
        var auction = await _db.AuctionSessions
            .Include(a => a.ReadyStates)
            .FirstOrDefaultAsync(a => a.LeagueId == leagueId && a.Status == AuctionStatus.Running, ct);

        if (auction == null)
            return new List<ReadyStateDto>();

        // Recupero i team per i nomi
        var teamIds = auction.ReadyStates.SelectMany(rs => rs.EligibleTeamIds).ToList();
        var teams = await _db.Teams
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        return auction.ReadyStates.SelectMany(rs => 
            rs.EligibleTeamIds.Select(teamId => new ReadyStateDto(
                TeamId: teamId,
                TeamName: teams.GetValueOrDefault(teamId, "Unknown"),
                IsReady: rs.ReadyTeamIds.Contains(teamId)
            ))
        ).ToList();
    }

    public async Task<IReadOnlyList<SerieAPlayer>> GetAvailablePlayersAsync(Guid leagueId, PlayerType role, CancellationToken ct = default)
    {
        // Recupero i giocatori già posseduti nella lega tramite i team della lega
        var teamIds = await _db.Teams
            .Where(t => t.LeagueId == leagueId)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var ownedPlayerIds = await _db.PlayerOwnerships
            .Where(po => teamIds.Contains(po.TeamId) && po.IsActive)
            .Select(po => po.SerieAPlayerId)
            .ToListAsync(ct);

        
        // Recupero tutti i giocatori del ruolo che non sono ancora posseduti
        var availablePlayers = await _db.SerieAPlayers
            .Where(p => p.PlayerType == role && !ownedPlayerIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return availablePlayers;
    }

    public async Task<AuctionOverviewDto?> GetAuctionOverviewAsync(Guid leagueId, CancellationToken ct = default)
    {
        var league = await _db.Leagues
            .Include(l => l.ActiveAuction)
            .Include(l => l.Teams)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct);

        if (league?.ActiveAuction == null)
            return null;

        var auction = league.ActiveAuction;
        var teams = league.Teams.OrderBy(t => t.Name).ToList();
        
        // Costruisco il turn order con i team ordinati come definito nell'asta
        var turnOrder = new List<TurnOrderDto>();
        for (int i = 0; i < auction.TeamOrder.Count; i++)
        {
            var teamId = auction.TeamOrder[i];
            var team = teams.FirstOrDefault(t => t.Id == teamId);
            if (team != null)
            {
                turnOrder.Add(new TurnOrderDto(
                    Position: i + 1,
                    TeamId: teamId,
                    TeamName: team.Name,
                    IsCurrentTurn: i == auction.CurrentOrderIndex
                ));
            }
        }

        var currentTurnTeamId = auction.TeamOrder.ElementAtOrDefault(auction.CurrentOrderIndex);
        var currentTurnTeam = teams.FirstOrDefault(t => t.Id == currentTurnTeamId);

        return new AuctionOverviewDto(
            AuctionId: auction.Id,
            LeagueName: league.Name,
            Status: auction.Status,
            CurrentRole: auction.CurrentRole,
            CurrentTurnPosition: auction.CurrentOrderIndex + 1,
            TotalTeams: teams.Count,
            CurrentTurnTeamId: currentTurnTeamId,
            CurrentTurnTeamName: currentTurnTeam?.Name ?? "N/A",
            TurnOrder: turnOrder,
            IsBiddingActive: auction.IsBiddingActive,
            IsReadyCheckActive: auction.CurrentReadyState != null && !auction.CurrentReadyState.IsCompleted
        );
    }

    public async Task<IReadOnlyList<TurnOrderDto>> GetTurnOrderAsync(Guid leagueId, CancellationToken ct = default)
    {
        var league = await _db.Leagues
            .Include(l => l.ActiveAuction)
            .Include(l => l.Teams)
            .FirstOrDefaultAsync(l => l.Id == leagueId, ct);

        if (league?.ActiveAuction == null)
            return new List<TurnOrderDto>();

        var auction = league.ActiveAuction;
        var teams = league.Teams.ToDictionary(t => t.Id, t => t.Name);
        
        var turnOrder = new List<TurnOrderDto>();
        for (int i = 0; i < auction.TeamOrder.Count; i++)
        {
            var teamId = auction.TeamOrder[i];
            if (teams.TryGetValue(teamId, out var teamName))
            {
                turnOrder.Add(new TurnOrderDto(
                    Position: i + 1,
                    TeamId: teamId,
                    TeamName: teamName,
                    IsCurrentTurn: i == auction.CurrentOrderIndex
                ));
            }
        }

        return turnOrder;
    }
}
