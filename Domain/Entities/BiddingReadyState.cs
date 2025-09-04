namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;

/// <summary>
/// Entità per persistere lo stato di ready-check durante la fase di offerta
/// Traccia quali team sono pronti per la fase di offerta
/// </summary>
public sealed class BiddingReadyState : BaseEntity<Guid>
{
    private readonly List<Guid> _readyTeamIds = new();

    public Guid SessionId { get; private set; }
    public Guid NominatorTeamId { get; private set; }
    public int SerieAPlayerId { get; private set; }
    public PlayerType Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Team che devono confermare ready (escluso il nominatore)
    /// </summary>
    public IReadOnlyList<Guid> EligibleTeamIds { get; private set; } = new List<Guid>();

    /// <summary>
    /// Team che hanno confermato ready
    /// </summary>
    public IReadOnlyList<Guid> ReadyTeamIds => _readyTeamIds.AsReadOnly();
    
    /// <summary>
    /// Proprietà per EF Core per persistere i team ready
    /// </summary>
    public List<Guid> ReadyTeamIdsForPersistence 
    { 
        get => _readyTeamIds; 
        private set => _readyTeamIds.AddRange(value); 
    }

    /// <summary>
    /// Percentuale di completamento (team pronti / team eleggibili)
    /// </summary>
    public decimal CompletionPercentage => EligibleTeamIds.Count == 0 
        ? 1.0m 
        : (decimal)_readyTeamIds.Count / EligibleTeamIds.Count;

    /// <summary>
    /// Tutti i team hanno confermato ready
    /// </summary>
    public bool AllTeamsReady => EligibleTeamIds.All(id => _readyTeamIds.Contains(id));

    // EF Core constructor
    private BiddingReadyState() { }

    private BiddingReadyState(
        Guid sessionId,
        Guid nominatorTeamId,
        int serieAPlayerId,
        PlayerType role,
        IReadOnlyList<Guid> eligibleTeamIds)
    {
        Id = Guid.NewGuid();
        SessionId = sessionId;
        NominatorTeamId = nominatorTeamId;
        SerieAPlayerId = serieAPlayerId;
        Role = role;
        EligibleTeamIds = eligibleTeamIds.ToList();
        CreatedAt = DateTime.UtcNow;
        IsCompleted = false;
    }

    /// <summary>
    /// Crea un nuovo stato di ready-check
    /// </summary>
    public static BiddingReadyState Create(
        Guid sessionId,
        Guid nominatorTeamId,
        int serieAPlayerId,
        PlayerType role,
        IReadOnlyList<Guid> eligibleTeamIds)
    {
        return new BiddingReadyState(sessionId, nominatorTeamId, serieAPlayerId, role, eligibleTeamIds);
    }

    /// <summary>
    /// Marca un team come pronto
    /// </summary>
    public bool MarkTeamReady(Guid teamId)
    {
        if (IsCompleted) return false;
        if (!EligibleTeamIds.Contains(teamId)) return false;
        if (_readyTeamIds.Contains(teamId)) return false; // Già pronto

        _readyTeamIds.Add(teamId);
        return true;
    }

    /// <summary>
    /// Rimuove il ready di un team (se cambia idea)
    /// </summary>
    public bool UnmarkTeamReady(Guid teamId)
    {
        if (IsCompleted) return false;
        return _readyTeamIds.Remove(teamId);
    }

    /// <summary>
    /// Completa il ready-check (quando tutti sono pronti o timeout)
    /// </summary>
    public void Complete()
    {
        IsCompleted = true;
    }

    /// <summary>
    /// Verifica se un team è pronto
    /// </summary>
    public bool IsTeamReady(Guid teamId)
    {
        return _readyTeamIds.Contains(teamId);
    }

    /// <summary>
    /// Team mancanti per completare il ready-check
    /// </summary>
    public IReadOnlyList<Guid> GetPendingTeamIds()
    {
        return EligibleTeamIds.Where(id => !_readyTeamIds.Contains(id)).ToList();
    }
}
