namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Events;
using Domain.Services;

public class AuctionSession : AggregateRoot
{
    public Guid LeagueId { get; private set; }
    public AuctionStatus Status { get; private set; } = AuctionStatus.Preparation;
    public RoleType CurrentRole { get; private set; } = RoleType.P;
    public int CurrentOrderIndex { get; private set; } = 0;

    public int BasePrice { get; private set; } = 1;
    public int MinIncrement { get; private set; } = 1;

    // Readiness tracking for bidding phase
    private HashSet<Guid> _eligibleForCurrentNomination = new();
    private HashSet<Guid> _readyTeamsForCurrentNomination = new();
    private Guid _currentNominatorTeamId;
    private int _currentSerieAPlayerId;
    private bool _biddingActive;
    private int _currentHighestBid;
    private Guid _currentHighestTeamId;

    // Expose current bidding state as persistable properties (EF uses private setters)
    public bool IsBiddingActive { get => _biddingActive; private set => _biddingActive = value; }
    public int CurrentHighestBid { get => _currentHighestBid; private set => _currentHighestBid = value; }
    public Guid CurrentHighestTeamId { get => _currentHighestTeamId; private set => _currentHighestTeamId = value; }
    public int CurrentSerieAPlayerId { get => _currentSerieAPlayerId; private set => _currentSerieAPlayerId = value; }

    private AuctionSession() { }

    public static AuctionSession Create(Guid leagueId, int basePrice = 1, int minIncrement = 1)
    {
        if (leagueId == Guid.Empty) throw new DomainException("LeagueId required");
        if (basePrice <= 0) throw new DomainException("Base price must be positive");
        if (minIncrement <= 0) throw new DomainException("Min increment must be positive");

        return new AuctionSession
        {
            LeagueId = leagueId,
            BasePrice = basePrice,
            MinIncrement = minIncrement
        };
    }

    public void Start()
    {
        Status = AuctionStatus.Running;
        RaiseDomainEvent(new AuctionSessionStarted(Id, LeagueId));
    }
    public void SetOrderStart(int startIndex) => CurrentOrderIndex = startIndex < 0 ? 0 : startIndex;
    public void AdvanceOrder(int count)
    {
        if (count <= 0) return;
        CurrentOrderIndex += count;
    }
    public void SetRole(RoleType role) => CurrentRole = role;
    public void Pause() => Status = AuctionStatus.Paused;
    public void ReviewPhase() => Status = AuctionStatus.Review;
    public void Complete() => Status = AuctionStatus.Completed;
    public void Cancel() => Status = AuctionStatus.Cancelled;

    // Nomination: decides auto-assign or bidding readiness. Requires current order and teams map externally.
    public void Nominate(
        IReadOnlyList<Guid> order,
        IReadOnlyDictionary<Guid, Team> teams,
        Guid nominatorTeamId,
        SerieAPlayer player)
    {
        if (Status != AuctionStatus.Running) throw new DomainException("Session not running");
        if (player is null) throw new DomainException("Player required");
        if (!teams.TryGetValue(nominatorTeamId, out var nominator)) throw new DomainException("Nominator not found");

    // Ensure previous round bidding state cleared (but do not touch order/role)
    _biddingActive = false;
    _currentHighestBid = 0;
    _currentHighestTeamId = Guid.Empty;

        var res = AuctionFlow.EvaluateNomination(teams.Values, nominatorTeamId, player.PlayerType switch
        {
            PlayerType.Goalkeeper => RoleType.P,
            PlayerType.Defender => RoleType.D,
            PlayerType.Midfielder => RoleType.C,
            PlayerType.Forward => RoleType.A,
            _ => throw new DomainException("Invalid role")
        });

    if (res.AutoAssign)
        {
            // Raise event; applicative layer will persist ownership and update team budget
            RaiseDomainEvent(new PlayerAutoAssigned(Id, nominatorTeamId, player.Id, CurrentRole, 1));

            // Advance order to next eligible for current role; if none, advance role
            var map = teams; // already provided
            var nextIdx = AuctionFlow.FindNextEligibleIndex(order, (CurrentOrderIndex + 1) % order.Count, map, CurrentRole);
            if (nextIdx >= 0 && nextIdx != CurrentOrderIndex)
            {
                CurrentOrderIndex = nextIdx;
                RaiseDomainEvent(new TurnAdvanced(Id, CurrentOrderIndex, CurrentRole));
            }
            else
            {
                var next = AuctionFlow.NextRole(CurrentRole);
                if (next is not null)
                {
                    CurrentRole = next.Value;
                    CurrentOrderIndex = 0;
                    RaiseDomainEvent(new RoleAdvanced(Id, CurrentRole));
                }
                else
                {
                    ReviewPhase(); // or Complete(); depends on flow
                }
            }
        }
        else
        {
            // Initialize readiness tracking
            _currentNominatorTeamId = nominatorTeamId;
            CurrentSerieAPlayerId = player.Id;
            _eligibleForCurrentNomination = res.EligibleOthers.ToHashSet();
            _readyTeamsForCurrentNomination.Clear();

            RaiseDomainEvent(new BiddingReadyRequested(Id, nominatorTeamId, player.Id, CurrentRole, res.EligibleOthers));
        }
    }

    // Mark a team as ready for current nomination; when all are ready, raise completion event
    public void MarkReady(Guid teamId)
    {
        if (Status != AuctionStatus.Running) throw new DomainException("Session not running");
        if (!_eligibleForCurrentNomination.Contains(teamId)) return; // ignore non-eligible
        _readyTeamsForCurrentNomination.Add(teamId);
        if (_readyTeamsForCurrentNomination.SetEquals(_eligibleForCurrentNomination))
        {
            IsBiddingActive = true;
            RaiseDomainEvent(new BiddingReadyCompleted(Id, _currentNominatorTeamId, _currentSerieAPlayerId, CurrentRole, _eligibleForCurrentNomination.ToList()));
        }
    }

    // Reset readiness state; to be called after a bidding round completes or is cancelled
    public void ResetReadiness()
    {
        _eligibleForCurrentNomination.Clear();
        _readyTeamsForCurrentNomination.Clear();
        _currentNominatorTeamId = Guid.Empty;
        _currentSerieAPlayerId = 0;
    IsBiddingActive = false;
    CurrentHighestBid = 0;
    CurrentHighestTeamId = Guid.Empty;
    }

    public void PlaceBid(Guid teamId, int amount)
    {
        if (Status != AuctionStatus.Running) throw new DomainException("Session not running");
    if (!IsBiddingActive) throw new DomainException("Bidding not active");
        // Allowed bidders: nominator + eligible others
        var allowed = _eligibleForCurrentNomination.Contains(teamId) || teamId == _currentNominatorTeamId;
        if (!allowed) throw new DomainException("Team not allowed to bid");

    var minRequired = CurrentHighestBid == 0 ? BasePrice : CurrentHighestBid + MinIncrement;
        if (amount < minRequired) throw new DomainException("Bid too low");

    CurrentHighestBid = amount;
    CurrentHighestTeamId = teamId;
        RaiseDomainEvent(new NewHighestBidPlaced(Id, _currentSerieAPlayerId, teamId, amount));
    }

    // Provide current winning bid snapshot for infrastructure if needed
    public (Guid TeamId, int Amount) GetCurrentWinningBid()
        => (_currentHighestTeamId, _currentHighestBid);

    // Advance the auction flow after a bidding round completes (with or without assignment)
    // This method updates CurrentOrderIndex/CurrentRole and raises the appropriate events, then resets readiness/bidding state.
    public void AdvanceAfterRound(IReadOnlyList<Guid> order, IReadOnlyDictionary<Guid, Team> teams)
    {
        if (order is null || order.Count == 0) throw new DomainException("Order required");
        if (teams is null) throw new DomainException("Teams required");

        var nextIdx = AuctionFlow.FindNextEligibleIndex(order, (CurrentOrderIndex + 1) % order.Count, teams, CurrentRole);
        if (nextIdx >= 0 && nextIdx != CurrentOrderIndex)
        {
            CurrentOrderIndex = nextIdx;
            RaiseDomainEvent(new TurnAdvanced(Id, CurrentOrderIndex, CurrentRole));
        }
        else
        {
            var next = AuctionFlow.NextRole(CurrentRole);
            if (next is not null)
            {
                CurrentRole = next.Value;
                CurrentOrderIndex = 0;
                RaiseDomainEvent(new RoleAdvanced(Id, CurrentRole));
            }
            else
            {
                ReviewPhase();
            }
        }

        ResetReadiness();
    }
}
