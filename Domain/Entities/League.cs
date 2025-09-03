namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Events;
using Domain.ValueObjects;

public class League : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    
    // Collezioni interne
    private readonly List<Team> _teams = new();
    private readonly List<PlayerOwnership> _playerOwnerships = new();
    
    public IReadOnlyList<Team> Teams => _teams.AsReadOnly();
    public IReadOnlyList<PlayerOwnership> PlayerOwnerships => _playerOwnerships.AsReadOnly();
    
    // Una sola asta attiva per volta
    public AuctionSession? ActiveAuction { get; private set; }

    private League() { }

    public static League Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("League name required");
        return new League { Name = name.Trim() };
    }

    #region Team Management

    public Team AddTeam(string teamName, int initialBudget)
    {
        if (string.IsNullOrWhiteSpace(teamName)) throw new DomainException("Team name required");
        if (initialBudget < 0) throw new DomainException("Initial budget must be non-negative");
        
        var normalizedName = teamName.Trim().ToLowerInvariant();
        if (_teams.Any(t => t.Name.ToLowerInvariant() == normalizedName))
            throw new DomainException("Team name already exists in this league");

        var team = Team.CreateInternal(Id, teamName.Trim(), initialBudget);
        _teams.Add(team);
        
        return team;
    }

    public Team GetTeam(Guid teamId)
    {
        return _teams.FirstOrDefault(t => t.Id == teamId) 
            ?? throw new DomainException("Team not found");
    }

    public IReadOnlyList<Team> GetTeamsWithSlotForRole(RoleType role)
    {
        return _teams.Where(t => t.HasSlot(role)).ToList();
    }

    #endregion

    #region Auction Lifecycle

    public void StartAuction(int basePrice = 1, int minIncrement = 1, IReadOnlyList<Guid>? customOrder = null)
    {
        if (ActiveAuction != null && ActiveAuction.CannotStart())
            throw new DomainException("Cannot start auction in current state");
            
        if (_teams.Count < 2)
            throw new DomainException("At least 2 teams required to start auction");

        var order = customOrder ?? _teams.Select(t => t.Id).ToList();
        ActiveAuction = AuctionSession.CreateInternal(Id, basePrice, minIncrement, order);
        ActiveAuction.Start();
        
        // Evento per UI e timer setup
        RaiseDomainEvent(new AuctionStarted(Id, ActiveAuction.Id, ActiveAuction.GetCurrentTurnInfo()));
    }

    public void PauseAuction()
    {
        if (ActiveAuction?.Status != AuctionStatus.Running)
            throw new DomainException("No running auction to pause");

        ActiveAuction.Pause();
        RaiseDomainEvent(new AuctionPaused(Id, ActiveAuction.Id));
    }

    public void ResumeAuction()
    {
        if (ActiveAuction?.Status != AuctionStatus.Paused)
            throw new DomainException("No paused auction to resume");

        ActiveAuction.Resume();
        var turnInfo = ActiveAuction.GetCurrentTurnInfo();
        RaiseDomainEvent(new AuctionResumed(Id, ActiveAuction.Id, turnInfo));
    }

    public void CompleteAuction()
    {
        if (ActiveAuction == null) throw new DomainException("No auction to complete");
        
        ActiveAuction.Complete();
        RaiseDomainEvent(new AuctionCompleted(Id, ActiveAuction.Id, GetLeagueStats()));
    }

    #endregion

    #region Nomination and Bidding

    /// <summary>
    /// Punto di ingresso unico per nominare un giocatore
    /// Responsabilità: validazione cross-entità, orchestrazione, consistenza transazionale
    /// </summary>
    public NominationResult NominatePlayer(Guid nominatorTeamId, SerieAPlayer player)
    {
        if (ActiveAuction?.Status != AuctionStatus.Running)
            throw new DomainException("No active auction");

        var nominator = GetTeam(nominatorTeamId);
        var teamsDict = _teams.ToDictionary(t => t.Id);
        
        // Delega ad AuctionSession per logica specifica dell'asta
        var result = ActiveAuction.ProcessNomination(nominatorTeamId, player, teamsDict);
        
        if (result.IsAutoAssign)
        {
            // Operazione transazionale: assegna + avanza
            AssignPlayerInternal(nominatorTeamId, player, result.Price, result.Role);
            var nextTurn = ActiveAuction.AdvanceToNextTurn(teamsDict);
            
            // Eventi per UI
            RaiseDomainEvent(new PlayerAssigned(Id, nominatorTeamId, player.Id, result.Price, nextTurn));
        }
        else
        {
            // Avvia bidding - eventi per timer e UI
            RaiseDomainEvent(new BiddingPhaseStarted(Id, result.BiddingInfo!));
        }
        
        return result;
    }

    /// <summary>
    /// Piazza offerta con validazione atomica budget
    /// Responsabilità: validazione budget, delegare ad AuctionSession
    /// </summary>
    public void PlaceBid(Guid teamId, int amount)
    {
        if (ActiveAuction?.Status != AuctionStatus.Running)
            throw new DomainException("No active auction");

        var team = GetTeam(teamId);
        if (team.Budget < amount)
            throw new DomainException($"Insufficient budget. Available: {team.Budget}, Required: {amount}");

        // Delega ad AuctionSession per logica bidding
        var bidResult = ActiveAuction.PlaceBid(teamId, amount);
        
        // Evento per aggiornamento UI in tempo reale
        RaiseDomainEvent(new BidPlaced(Id, teamId, amount, bidResult.TimeRemaining));
    }

    /// <summary>
    /// Finalizza round di bidding assegnando al vincitore
    /// Responsabilità: assegnazione transazionale + avanzamento asta
    /// </summary>
    public void FinalizeBiddingRound(SerieAPlayer player)
    {
        if (ActiveAuction?.IsBiddingActive != true)
            throw new DomainException("No active bidding to finalize");

        var winningBid = ActiveAuction.GetWinningBid();
        var role = DeterminePlayerRole(player);
        
        // Operazione atomica: assegna + avanza
        AssignPlayerInternal(winningBid.TeamId, player, winningBid.Amount, role);
        var nextTurn = ActiveAuction.FinalizeBidding(_teams.ToDictionary(t => t.Id));
        
        // Evento per UI e stop timer
        RaiseDomainEvent(new BiddingRoundFinalized(Id, winningBid.TeamId, player.Id, winningBid.Amount, nextTurn));
    }

    /// <summary>
    /// Forza avanzamento turno (per timeout o azioni admin)
    /// </summary>
    public void ForceTurnAdvancement()
    {
        if (ActiveAuction?.Status != AuctionStatus.Running)
            throw new DomainException("No active auction");

        var nextTurn = ActiveAuction.ForceAdvance(_teams.ToDictionary(t => t.Id));
        RaiseDomainEvent(new TurnForced(Id, nextTurn));
    }

    #endregion

    #region Admin Operations

    /// <summary>
    /// Annulla ultima assegnazione (per errori)
    /// </summary>
    public void UndoLastAssignment()
    {
        var lastOwnership = _playerOwnerships
            .Where(o => o.IsActive)
            .OrderByDescending(o => o.AcquiredAt)
            .FirstOrDefault();
            
        if (lastOwnership == null) throw new DomainException("No assignment to undo");

        var team = GetTeam(lastOwnership.TeamId);
        var role = DetermineRoleFromPlayerId(lastOwnership.SerieAPlayerId);
        
        team.ReleasePlayerInternal(role, lastOwnership.PurchasePrice);
        lastOwnership.DeactivateInternal("Undone by admin");
        
        RaiseDomainEvent(new AssignmentUndone(Id, lastOwnership.TeamId, lastOwnership.SerieAPlayerId));
    }

    /// <summary>
    /// Modifica ordine squadre durante asta (solo se necessario)
    /// </summary>
    public void UpdateAuctionOrder(IReadOnlyList<Guid> newOrder)
    {
        if (ActiveAuction == null) throw new DomainException("No active auction");
        
        ValidateOrderContainsAllTeams(newOrder);
        ActiveAuction.UpdateOrder(newOrder);
        
        RaiseDomainEvent(new AuctionOrderChanged(Id, newOrder));
    }

    #endregion

    #region Query Methods

    public AuctionState GetCurrentAuctionState()
    {
        if (ActiveAuction == null) return AuctionState.NoAuction();
        
        return new AuctionState(
            ActiveAuction.Status,
            ActiveAuction.GetCurrentTurnInfo(),
            ActiveAuction.GetBiddingInfo(),
            GetTeamsSummary()
        );
    }

    public LeagueStatistics GetLeagueStats()
    {
        return new LeagueStatistics(
            _teams.Count,
            _playerOwnerships.Count(o => o.IsActive),
            _teams.Sum(t => t.Budget),
            _playerOwnerships.Where(o => o.IsActive).Sum(o => o.PurchasePrice)
        );
    }

    public IReadOnlyList<TeamSummary> GetTeamsSummary()
    {
        return _teams.Select(t => new TeamSummary(
            t.Id,
            t.Name,
            t.Budget,
            t.GetPlayerCounts(),
            GetTeamOwnerships(t.Id).Count
        )).ToList();
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Operazione interna atomica per assegnazione giocatore
    /// </summary>
    private void AssignPlayerInternal(Guid teamId, SerieAPlayer player, int price, RoleType role)
    {
        var team = GetTeam(teamId);
        
        // Verifiche atomiche
        if (!team.HasSlot(role))
            throw new DomainException($"No slot available for role {role}");
        if (team.Budget < price)
            throw new DomainException($"Insufficient budget for team {team.Name}");

        // Modifica atomica
        team.AssignPlayerInternal(role, price);
        
        var ownership = PlayerOwnership.CreateInternal(
            teamId, player.Id, price, ActiveAuction?.Id ?? Guid.Empty);
        _playerOwnerships.Add(ownership);
    }

    private RoleType DeterminePlayerRole(SerieAPlayer player) => player.PlayerType switch
    {
        PlayerType.Goalkeeper => RoleType.P,
        PlayerType.Defender => RoleType.D,
        PlayerType.Midfielder => RoleType.C,
        PlayerType.Forward => RoleType.A,
        _ => throw new DomainException("Invalid player type")
    };

    private RoleType DetermineRoleFromPlayerId(int playerId)
    {
        // Per ora implementazione semplificata - andrà migliorata con lookup SerieAPlayer
        // Assumiamo che sia memorizzato nell'ownership o calcolabile in altro modo
        throw new NotImplementedException("Requires SerieAPlayer lookup");
    }

    private IReadOnlyList<PlayerOwnership> GetTeamOwnerships(Guid teamId)
    {
        return _playerOwnerships.Where(o => o.TeamId == teamId && o.IsActive).ToList();
    }

    private void ValidateOrderContainsAllTeams(IReadOnlyList<Guid> order)
    {
        var teamIds = _teams.Select(t => t.Id).ToHashSet();
        var orderIds = order.ToHashSet();
        
        if (!teamIds.SetEquals(orderIds))
            throw new DomainException("Order must contain all teams exactly once");
    }

    #endregion
}
