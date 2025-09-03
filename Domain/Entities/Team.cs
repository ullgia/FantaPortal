namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

public class Team : BaseEntity
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

    private Team() { }

    internal static Team CreateInternal(Guid leagueId, string name, int initialBudget)
    {
        return new Team { LeagueId = leagueId, Name = name, Budget = initialBudget };
    }

    #region Slot Management

    public bool HasSlot(RoleType role) => GetAvailableSlots(role) > 0;

    public int GetAvailableSlots(RoleType role) => role switch
    {
        RoleType.P => MaxP - CountP,
        RoleType.D => MaxD - CountD,
        RoleType.C => MaxC - CountC, 
        RoleType.A => MaxA - CountA,
        _ => 0
    };

    public PlayerCounts GetPlayerCounts() => new(CountP, CountD, CountC, CountA);

    #endregion

    #region Internal Operations (solo da League)

    /// <summary>
    /// Assegna giocatore - solo League può chiamare
    /// Responsabilità: aggiornamento atomico budget e contatori
    /// </summary>
    internal void AssignPlayerInternal(RoleType role, int price)
    {
        if (price > Budget) throw new DomainException("Insufficient budget");
        if (!HasSlot(role)) throw new DomainException("No available slot");
        
        Budget -= price;
        IncrementRoleCount(role);
    }

    /// <summary>
    /// Rilascia giocatore (per undo/correzioni)
    /// </summary>
    internal void ReleasePlayerInternal(RoleType role, int refund)
    {
        DecrementRoleCount(role);
        Budget += refund;
    }

    #endregion

    #region Private Helpers

    private void IncrementRoleCount(RoleType role)
    {
        switch (role)
        {
            case RoleType.P: CountP++; break;
            case RoleType.D: CountD++; break;
            case RoleType.C: CountC++; break;
            case RoleType.A: CountA++; break;
        }
    }

    private void DecrementRoleCount(RoleType role)
    {
        switch (role)
        {
            case RoleType.P: 
                if (CountP <= 0) throw new DomainException("No P players to release");
                CountP--; break;
            case RoleType.D:
                if (CountD <= 0) throw new DomainException("No D players to release");
                CountD--; break;
            case RoleType.C:
                if (CountC <= 0) throw new DomainException("No C players to release");
                CountC--; break;
            case RoleType.A:
                if (CountA <= 0) throw new DomainException("No A players to release");
                CountA--; break;
        }
    }

    #endregion
}
