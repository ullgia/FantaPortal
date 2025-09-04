namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.Services;
using Domain.ValueObjects;

public class AuctionSession : AggregateRoot
{
    public Guid LeagueId { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Preparation;
    public PlayerType CurrentRole { get; private set; } = PlayerType.Goalkeeper;
    public int CurrentOrderIndex { get; private set; } = 0;
    
    public int BasePrice { get; private set; } = 1;
    public int MinIncrement { get; private set; } = 1;
    
    // Stato ordine squadre
    private readonly List<AuctionSessionTurnOrder> _turnOrders = new();
    public IReadOnlyList<AuctionSessionTurnOrder> TurnOrders => _turnOrders.AsReadOnly();
    public IReadOnlyList<Guid> TeamOrder => _turnOrders.OrderBy(to => to.Position).Select(to => to.TeamId).ToList();
    
    // Stato bidding corrente
    private BiddingState _currentBidding = BiddingState.Empty;
    
    // Stato ready-check corrente
    private readonly List<BiddingReadyState> _readyStates = new();
    public IReadOnlyList<BiddingReadyState> ReadyStates => _readyStates.AsReadOnly();
    public BiddingReadyState? CurrentReadyState => _readyStates.FirstOrDefault(rs => !rs.IsCompleted);
    
    // Proprietà per persistenza
    public bool IsBiddingActive => _currentBidding.IsActive;
    public int CurrentSerieAPlayerId => _currentBidding.PlayerId;

    private AuctionSession() { }

    internal static AuctionSession CreateInternal(Guid leagueId, int basePrice, int minIncrement, IReadOnlyList<Guid> teamOrder)
    {
        var session = new AuctionSession
        {
            LeagueId = leagueId,
            BasePrice = basePrice,
            MinIncrement = minIncrement
        };
        
        // Crea l'ordine dei turni
        for (int i = 0; i < teamOrder.Count; i++)
        {
            session._turnOrders.Add(AuctionSessionTurnOrder.Create(session.Id, teamOrder[i], i));
        }
        
        return session;
    }

    #region State Management

    internal void Start() => Status = AuctionStatus.Running;
    internal void Pause() => Status = AuctionStatus.Paused;  
    internal void Resume() => Status = AuctionStatus.Running;
    internal void Complete() => Status = AuctionStatus.Completed;
    
    internal bool CannotStart() => Status == AuctionStatus.Running;

    // Temporary method for backward compatibility with tests
    internal void MarkReady(Guid teamId)
    {
        var currentReady = CurrentReadyState;
        if (currentReady != null && currentReady.MarkTeamReady(teamId))
        {
            // Verifica se tutti sono pronti
            if (currentReady.AllTeamsReady)
            {
                currentReady.Complete();
                RaiseDomainEvent(new BiddingReadyCompleted(
                    Id, 
                    currentReady.NominatorTeamId, 
                    currentReady.SerieAPlayerId, 
                    currentReady.Role, 
                    currentReady.EligibleTeamIds));
            }
        }
    }

    /// <summary>
    /// Inizia una nuova fase di ready-check per la nomina
    /// </summary>
    internal BiddingReadyState StartReadyCheck(Guid nominatorTeamId, int serieAPlayerId, PlayerType role, IReadOnlyList<Guid> eligibleTeamIds)
    {
        // Completa eventuali ready-check precedenti
        var currentReady = CurrentReadyState;
        currentReady?.Complete();

        // Crea nuovo ready-check
        var newReadyState = BiddingReadyState.Create(Id, nominatorTeamId, serieAPlayerId, role, eligibleTeamIds);
        _readyStates.Add(newReadyState);

        // Pubblica evento di inizio ready-check
        RaiseDomainEvent(new BiddingReadyRequested(Id, nominatorTeamId, serieAPlayerId, role, eligibleTeamIds));

        return newReadyState;
    }

    /// <summary>
    /// Conferma ready di un team
    /// </summary>
    internal bool ConfirmTeamReady(Guid teamId)
    {
        var currentReady = CurrentReadyState;
        if (currentReady?.MarkTeamReady(teamId) == true)
        {
            // Verifica se tutti sono pronti
            if (currentReady.AllTeamsReady)
            {
                currentReady.Complete();
                RaiseDomainEvent(new BiddingReadyCompleted(
                    Id, 
                    currentReady.NominatorTeamId, 
                    currentReady.SerieAPlayerId, 
                    currentReady.Role, 
                    currentReady.EligibleTeamIds));
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Rimuove ready di un team (se cambia idea)
    /// </summary>
    internal bool UnconfirmTeamReady(Guid teamId)
    {
        var currentReady = CurrentReadyState;
        return currentReady?.UnmarkTeamReady(teamId) == true;
    }

    /// <summary>
    /// Forza completamento ready-check (timeout o admin)
    /// </summary>
    internal void ForceCompleteReadyCheck()
    {
        var currentReady = CurrentReadyState;
        if (currentReady != null)
        {
            currentReady.Complete();
            RaiseDomainEvent(new BiddingReadyCompleted(
                Id, 
                currentReady.NominatorTeamId, 
                currentReady.SerieAPlayerId, 
                currentReady.Role, 
                currentReady.EligibleTeamIds));
        }
    }

    // Temporary method for backward compatibility with tests  
    internal NominationResult Nominate(IReadOnlyList<Guid> teamOrder, IReadOnlyDictionary<Guid, Team> teams, Guid nominatorId, SerieAPlayer player)
    {
        // Ricostruisce l'ordine dei turni
        _turnOrders.Clear();
        for (int i = 0; i < teamOrder.Count; i++)
        {
            _turnOrders.Add(AuctionSessionTurnOrder.Create(Id, teamOrder[i], i));
        }
        return ProcessNomination(nominatorId, player, teams);
    }

    #endregion

    #region Turn Logic

    /// <summary>
    /// Processa nomination delegata da League
    /// Responsabilità: logica auto-assign vs bidding, calcolo eligibili
    /// </summary>
    internal NominationResult ProcessNomination(Guid nominatorTeamId, SerieAPlayer player, IReadOnlyDictionary<Guid, Team> teams)
    {
        if (Status != AuctionStatus.Running) throw new DomainException("Auction not running");

        var role = player.PlayerType;
        var evaluation = AuctionFlow.EvaluateNomination(teams.Values, nominatorTeamId, role);
        
        if (evaluation.AutoAssign)
        {
            return NominationResult.AutoAssign(role, BasePrice);
        }
        else
        {
            // Inizia il ready-check prima del bidding
            StartReadyCheck(nominatorTeamId, player.Id, role, evaluation.EligibleOthers);
            
            // Il bidding inizierà dopo che tutti confermano ready
            // Per ora restituiamo informazioni sulla fase di ready-check
            return NominationResult.StartReadyCheck(role, evaluation.EligibleOthers, CurrentReadyState!);
        }
    }

    /// <summary>
    /// Inizia effettivamente il bidding dopo il completamento del ready-check
    /// </summary>
    internal BiddingInfo StartBiddingAfterReady()
    {
        // Prende l'ultimo ready state (che dovrebbe essere completato)
        var readyState = _readyStates.LastOrDefault();
        if (readyState == null || !readyState.IsCompleted)
        {
            throw new DomainException("Ready check not completed");
        }

        _currentBidding = BiddingState.Start(
            readyState.NominatorTeamId, 
            readyState.SerieAPlayerId, 
            BasePrice, 
            readyState.EligibleTeamIds);
            
        return CreateBiddingInfo();
    }

    /// <summary>
    /// Avanza al prossimo turno
    /// Responsabilità: calcolo prossimo team/ruolo, aggiornamento stato
    /// </summary>
    internal TurnInfo AdvanceToNextTurn(IReadOnlyDictionary<Guid, Team> teams)
    {
        var (nextRole, nextIndex) = AuctionFlow.AdvanceUntilEligible(
            TeamOrder, teams, CurrentRole, CurrentOrderIndex);
            
        if (nextRole.HasValue)
        {
            CurrentRole = nextRole.Value;
            CurrentOrderIndex = nextIndex;
        }
        else
        {
            Status = AuctionStatus.Review; // Completato
        }
        
        return GetCurrentTurnInfo();
    }

    /// <summary>
    /// Forza avanzamento (timeout/admin)
    /// </summary>
    internal TurnInfo ForceAdvance(IReadOnlyDictionary<Guid, Team> teams)
    {
        _currentBidding = BiddingState.Empty;
        return AdvanceToNextTurn(teams);
    }

    #endregion

    #region Bidding Logic

    /// <summary>
    /// Piazza offerta - solo logica di validazione bidding
    /// Responsabilità: regole offerte, aggiornamento stato bidding
    /// </summary>
    internal BidResult PlaceBid(Guid teamId, int amount)
    {
        if (!_currentBidding.IsActive) throw new DomainException("No active bidding");
        if (!_currentBidding.CanBid(teamId)) throw new DomainException("Team cannot bid");
        
        // First bid must be at least base price, subsequent bids must have minimum increment
        var isFirstBid = _currentBidding.HighestBid == BasePrice && _currentBidding.HighestBidder == _currentBidding.NominatorId;
        var minRequired = isFirstBid ? BasePrice : _currentBidding.HighestBid + MinIncrement;
        
        if (amount < minRequired) throw new DomainException($"Bid too low");
        
        _currentBidding = _currentBidding.WithNewBid(teamId, amount);
        
        return new BidResult(amount, EstimateRemainingTime());
    }

    /// <summary>
    /// Finalizza bidding e avanza
    /// </summary>
    internal TurnInfo FinalizeBidding(IReadOnlyDictionary<Guid, Team> teams)
    {
        if (!_currentBidding.IsActive) throw new DomainException("No bidding to finalize");
        
        _currentBidding = BiddingState.Empty;
        return AdvanceToNextTurn(teams);
    }

    internal WinningBid GetWinningBid()
    {
        if (!_currentBidding.IsActive) throw new DomainException("No active bidding");
        return new WinningBid(_currentBidding.HighestBidder, _currentBidding.HighestBid);
    }

    #endregion

    #region Order Management

    internal void UpdateOrder(IReadOnlyList<Guid> newOrder)
    {
        // Ricostruisce l'ordine dei turni
        _turnOrders.Clear();
        for (int i = 0; i < newOrder.Count; i++)
        {
            _turnOrders.Add(AuctionSessionTurnOrder.Create(Id, newOrder[i], i));
        }
        // Potrebbe richiedere ricalcolo CurrentOrderIndex
    }

    #endregion

    #region Query Methods

    internal TurnInfo GetCurrentTurnInfo()
    {
        if (TeamOrder.Count == 0) return TurnInfo.Empty;
        
        var currentTeamId = TeamOrder[CurrentOrderIndex];
        return new TurnInfo(currentTeamId, CurrentRole, CurrentOrderIndex, Status);
    }

    internal BiddingInfo CreateBiddingInfo()
    {
        return new BiddingInfo(
            _currentBidding.NominatorId,
            _currentBidding.PlayerId,
            _currentBidding.HighestBidder,
            _currentBidding.HighestBid,
            _currentBidding.EligibleTeams.ToList(),
            EstimateRemainingTime()
        );
    }

    internal BiddingInfo? GetBiddingInfo()
    {
        return _currentBidding.IsActive ? CreateBiddingInfo() : null;
    }

    #endregion

    #region Private Helpers


    private int EstimateRemainingTime()
    {
        // Logica per stimare tempo rimanente basata su timer esterni
        return 30; // Default 30 seconds
    }

    #endregion
}

/// <summary>
/// Value object per stato bidding interno
/// </summary>
internal record BiddingState(
    bool IsActive,
    Guid NominatorId,
    int PlayerId,
    Guid HighestBidder,
    int HighestBid,
    HashSet<Guid> EligibleTeams)
{
    internal static BiddingState Empty => new(false, Guid.Empty, 0, Guid.Empty, 0, new());
    
    internal static BiddingState Start(Guid nominatorId, int playerId, int basePrice, IEnumerable<Guid> eligible)
    {
        var eligibleSet = eligible.ToHashSet();
        eligibleSet.Add(nominatorId);
        return new BiddingState(true, nominatorId, playerId, nominatorId, basePrice, eligibleSet);
    }
    
    internal bool CanBid(Guid teamId) => IsActive && EligibleTeams.Contains(teamId);
    
    internal BiddingState WithNewBid(Guid teamId, int amount) => 
        this with { HighestBidder = teamId, HighestBid = amount };
}
