namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

public class LeaguePlayer : BaseEntity
{
    public Guid LeagueId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Budget { get; private set; }

    // Limiti ruoli
    public int MaxP { get; private set; } = 3;
    public int MaxD { get; private set; } = 8; 
    public int MaxC { get; private set; } = 8;
    public int MaxA { get; private set; } = 6;

    // Contatori attuali
    public int CountP { get; private set; }
    public int CountD { get; private set; }
    public int CountC { get; private set; }
    public int CountA { get; private set; }

    private LeaguePlayer() { }

    internal static LeaguePlayer CreateInternal(Guid leagueId, string name, int initialBudget)
    {
        return new LeaguePlayer { LeagueId = leagueId, Name = name, Budget = initialBudget };
    }

    #region Slot Management

    public bool HasSlot(PlayerType role) => GetAvailableSlots(role) > 0;

    public int GetAvailableSlots(PlayerType role) => role switch
    {
        PlayerType.Goalkeeper => MaxP - CountP,
        PlayerType.Defender => MaxD - CountD,
        PlayerType.Midfielder => MaxC - CountC,
        PlayerType.Forward => MaxA - CountA,
        _ => 0
    };

    public PlayerCounts GetPlayerCounts() => new(CountP, CountD, CountC, CountA);

    #endregion

    #region Internal Operations (solo da League)

    /// <summary>
    /// Assegna giocatore - solo League può chiamare
    /// Responsabilità: aggiornamento atomico budget e contatori
    /// </summary>
    internal void AssignPlayerInternal(PlayerType role, int price)
    {
        if (price > Budget) throw new DomainException("Insufficient budget");
        if (!HasSlot(role)) throw new DomainException("No available slot");
        
        Budget -= price;
        IncrementRoleCount(role);
    }

    /// <summary>
    /// Rilascia giocatore (per undo/correzioni)
    /// </summary>
    internal void ReleasePlayerInternal(PlayerType role, int refund)
    {
        DecrementRoleCount(role);
        Budget += refund;
    }

    #endregion

    #region Private Helpers

    private void IncrementRoleCount(PlayerType role)
    {
        switch (role)
        {
            case PlayerType.Goalkeeper: CountP++; break;
            case PlayerType.Defender: CountD++; break;
            case PlayerType.Midfielder: CountC++; break;
            case PlayerType.Forward: CountA++; break;
        }
    }

    private void DecrementRoleCount(PlayerType role)
    {
        switch (role)
        {
            case PlayerType.Goalkeeper: 
                if (CountP <= 0) throw new DomainException("No Goalkeeper players to release");
                CountP--; break;
            case PlayerType.Defender:
                if (CountD <= 0) throw new DomainException("No Defender players to release");
                CountD--; break;
            case PlayerType.Midfielder:
                if (CountC <= 0) throw new DomainException("No Central Midfielder players to release");
                CountC--; break;
            case PlayerType.Forward:
                if (CountA <= 0) throw new DomainException("No Forward players to release");
                CountA--; break;
        }
    }

    #endregion
}
