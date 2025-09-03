using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portal.Services;

public sealed class AuctionRealtimeStore : IAsyncDisposable
{
    private readonly AuctionHubClient _hub;
    private bool _started;
    private Guid _auctionId;
    private Guid _leagueId;
    private Guid _sessionId;
    private Guid _joinedTurnId;
    private ScopeType _scope;

    public Guid AuctionId => _auctionId;
    public Guid LeagueId => _leagueId;
    public Guid SessionId => _sessionId;
    public Guid TurnId { get; private set; }
    public int RemainingSeconds { get; private set; }
    public int LastWarning { get; private set; }
    public (Guid TeamId, int Amount)? HighestBid { get; private set; }
    public string? LastEvent { get; private set; }
    public int? CurrentPlayerId { get; private set; }
    public string? CurrentRole { get; private set; }
    public Guid[]? EligibleOtherTeamIds { get; private set; }
    public Guid? NominatorTeamId { get; private set; }
    public bool IsReadyPhase { get; private set; }
    public bool IsBiddingPhase { get; private set; }

    public event Action? StateChanged;
    public event Action<int>? Warning; // remaining seconds

    public AuctionRealtimeStore(AuctionHubClient hub)
    {
        _hub = hub;
    }

    public async Task StartAsync(Guid auctionId, CancellationToken ct = default)
    {
        if (_started && _auctionId == auctionId) return;

        if (!_started)
        {
            _hub.TimerUpdate += OnTimerUpdate;
            _hub.TimerWarning += OnTimerWarning;
            await _hub.StartAsync(ct);
        }

        _auctionId = auctionId;
        _scope = ScopeType.Auction;
        await _hub.JoinAuctionAsync(auctionId);
        _started = true;
    }

    public async Task StartLeagueAsync(Guid leagueId, CancellationToken ct = default)
    {
        if (_started && _leagueId == leagueId && _scope == ScopeType.League) return;

        if (!_started)
        {
            _hub.TimerUpdate += OnTimerUpdate;
            _hub.TimerWarning += OnTimerWarning;
            _hub.NewHighestBid += OnNewHighestBid;
            _hub.ReadyRequested += OnReadyRequested;
            _hub.ReadyCompleted += OnReadyCompleted;
            _hub.PlayerAssigned += OnPlayerAssigned;
            _hub.TurnAdvanced += OnTurnAdvanced;
            _hub.RoleAdvanced += OnRoleAdvanced;
            await _hub.StartAsync(ct);
        }

        _leagueId = leagueId;
        _scope = ScopeType.League;
        await _hub.JoinLeagueAsync(leagueId);
        _started = true;
    }

    public async Task StartSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (_started && _sessionId == sessionId && _scope == ScopeType.Session) return;
        if (!_started)
        {
            _hub.TimerUpdate += OnTimerUpdate;
            _hub.TimerWarning += OnTimerWarning;
            _hub.NewHighestBid += OnNewHighestBid;
            _hub.ReadyRequested += OnReadyRequested;
            _hub.ReadyCompleted += OnReadyCompleted;
            _hub.PlayerAssigned += OnPlayerAssigned;
            _hub.TurnAdvanced += OnTurnAdvanced;
            _hub.RoleAdvanced += OnRoleAdvanced;
            await _hub.StartAsync(ct);
        }
        _sessionId = sessionId;
        _scope = ScopeType.Session;
        await _hub.JoinSessionAsync(sessionId);
        _started = true;
    }

    public async Task LeaveAsync()
    {
        if (_started)
        {
            try
            {
                if (_auctionId != Guid.Empty) await _hub.LeaveAuctionAsync(_auctionId);
                if (_leagueId != Guid.Empty) await _hub.LeaveLeagueAsync(_leagueId);
                if (_sessionId != Guid.Empty) await _hub.LeaveSessionAsync(_sessionId);
            }
            catch { }
        }
    }

    private void OnTimerUpdate(Guid leagueId, Guid auctionId, Guid turnId, int remaining)
    {
        if (!MatchesScope(leagueId, auctionId)) return;
        if (_joinedTurnId != Guid.Empty && _joinedTurnId != turnId)
        {
            // leave previous turn group
            _ = _hub.LeaveTurnAsync(_joinedTurnId);
        }
        if (_joinedTurnId != turnId)
        {
            _joinedTurnId = turnId;
            _ = _hub.JoinTurnAsync(turnId);
        }
        TurnId = turnId;
        RemainingSeconds = remaining;
        StateChanged?.Invoke();
    }

    private void OnTimerWarning(Guid leagueId, Guid auctionId, Guid turnId, int remaining)
    {
        if (!MatchesScope(leagueId, auctionId)) return;
        LastWarning = remaining;
        Warning?.Invoke(remaining);
    }

    private void OnNewHighestBid(Guid sessionId, int playerId, Guid teamId, int amount)
    {
        if (_scope == ScopeType.Session && sessionId != _sessionId) return;
        HighestBid = (teamId, amount);
        LastEvent = $"Nuova offerta: {amount} crediti";
        StateChanged?.Invoke();
    }

    private void OnReadyRequested(Guid sessionId, Guid nominatorTeamId, int playerId, string role, Guid[] eligibleTeamIds)
    {
        if (_scope == ScopeType.Session && sessionId != _sessionId) return;
        CurrentPlayerId = playerId;
        CurrentRole = role;
        EligibleOtherTeamIds = eligibleTeamIds;
        NominatorTeamId = nominatorTeamId;
        IsReadyPhase = true;
        IsBiddingPhase = false;
        LastEvent = $"Ready richiesto per giocatore {playerId} ({role})";
        HighestBid = null;
        StateChanged?.Invoke();
    }

    private void OnReadyCompleted(Guid sessionId, Guid nominatorTeamId, int playerId, string role, Guid[] eligibleTeamIds)
    {
        if (_scope == ScopeType.Session && sessionId != _sessionId) return;
        CurrentPlayerId = playerId;
        CurrentRole = role;
        EligibleOtherTeamIds = eligibleTeamIds;
    NominatorTeamId = nominatorTeamId;
    IsReadyPhase = false;
    IsBiddingPhase = true;
        LastEvent = $"Ready completato: via alle offerte per {playerId} ({role})";
        HighestBid = null;
        StateChanged?.Invoke();
    }

    private void OnPlayerAssigned(Guid sessionId, int playerId, Guid teamId, int amount)
    {
        if (_scope == ScopeType.Session && sessionId != _sessionId) return;
        HighestBid = null;
        LastEvent = $"Giocatore assegnato a squadra {teamId} per {amount}";
    IsReadyPhase = false;
    IsBiddingPhase = false;
        StateChanged?.Invoke();
    }

    private void OnTurnAdvanced(Guid sessionId, int newOrderIndex, string role)
    {
        if (_scope == ScopeType.Session && sessionId != _sessionId) return;
        LastEvent = $"Turno avanzato (ordine {newOrderIndex}, ruolo {role})";
        StateChanged?.Invoke();
    }

    private void OnRoleAdvanced(Guid sessionId, string newRole)
    {
        if (_scope == ScopeType.Session && sessionId != _sessionId) return;
        LastEvent = $"Ruolo avanzato: {newRole}";
        StateChanged?.Invoke();
    }

    // Command helpers
    public Task MarkReadyAsync(Guid teamId)
        => _hub.MarkReadyAsync(_sessionId, teamId);

    public Task PlaceBidAsync(Guid teamId, int amount)
        => _hub.PlaceBidAsync(_sessionId, teamId, amount);

    public async ValueTask DisposeAsync()
    {
        _hub.TimerUpdate -= OnTimerUpdate;
        _hub.TimerWarning -= OnTimerWarning;
    _hub.NewHighestBid -= OnNewHighestBid;
    _hub.ReadyRequested -= OnReadyRequested;
    _hub.ReadyCompleted -= OnReadyCompleted;
    _hub.PlayerAssigned -= OnPlayerAssigned;
    _hub.TurnAdvanced -= OnTurnAdvanced;
    _hub.RoleAdvanced -= OnRoleAdvanced;
        await LeaveAsync();
    }

    private bool MatchesScope(Guid leagueId, Guid auctionId)
        => _scope == ScopeType.Auction ? (auctionId == _auctionId) : (_scope == ScopeType.League ? (leagueId == _leagueId) : true);

    private enum ScopeType
    {
        Auction,
    League,
    Session
    }
}
