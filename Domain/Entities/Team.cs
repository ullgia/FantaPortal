namespace Domain.Entities;

using Domain.Common;
using Domain.Contracts;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Events;

public class Team : AggregateRoot
{
    public Guid LeagueId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Budget { get; private set; }

    public int MaxP { get; private set; } = 3;
    public int MaxD { get; private set; } = 8;
    public int MaxC { get; private set; } = 8;
    public int MaxA { get; private set; } = 6;

    public int CountP { get; private set; }
    public int CountD { get; private set; }
    public int CountC { get; private set; }
    public int CountA { get; private set; }

    private Team() { }

    public static async Task<Team> CreateAsync(Guid leagueId, string name, int initialBudget, ITeamValidator validator, CancellationToken ct = default)
    {
    if (leagueId == Guid.Empty) throw new DomainException("LeagueId required");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Team name required");
        if (initialBudget < 0) throw new DomainException("Initial budget must be non-negative");
        if (validator is null) throw new DomainException("Validator required");

    var trimmed = name.Trim();
    var normalized = trimmed.ToLowerInvariant();
    var unique = await validator.IsTeamNameUniqueAsync(leagueId, normalized, ct);
        if (!unique) throw new DomainException("Team name already exists in this league");

    var team = new Team
        {
            LeagueId = leagueId,
            Name = trimmed,
            Budget = initialBudget
        };
    team.RaiseDomainEvent(new TeamCreated(team.Id, team.LeagueId, team.Name));
    return team;
    }

    public bool HasSlot(RoleType role) => role switch
    {
        RoleType.P => CountP < MaxP,
        RoleType.D => CountD < MaxD,
        RoleType.C => CountC < MaxC,
        RoleType.A => CountA < MaxA,
        _ => false
    };

    public void Assign(RoleType role, int price)
    {
    if (price <= 0) throw new DomainException("Price must be positive");
        if (price > Budget) throw new DomainException("Insufficient budget");
        if (!HasSlot(role)) throw new DomainException("No slot available for role");
        Budget -= price;
        switch (role)
        {
            case RoleType.P: CountP++; break;
            case RoleType.D: CountD++; break;
            case RoleType.C: CountC++; break;
            case RoleType.A: CountA++; break;
        }
    }

    // Svincola un giocatore del ruolo indicato e applica un eventuale rimborso budget.
    // Nota: la logica di rimborso (percentuale, zero, ecc.) è demandata al chiamante/test.
    public void Release(RoleType role, int refund)
    {
        if (refund < 0) throw new DomainException("Refund must be non-negative");
        switch (role)
        {
            case RoleType.P:
                if (CountP <= 0) throw new DomainException("No players to release for role");
                CountP--;
                break;
            case RoleType.D:
                if (CountD <= 0) throw new DomainException("No players to release for role");
                CountD--;
                break;
            case RoleType.C:
                if (CountC <= 0) throw new DomainException("No players to release for role");
                CountC--;
                break;
            case RoleType.A:
                if (CountA <= 0) throw new DomainException("No players to release for role");
                CountA--;
                break;
            default:
                throw new DomainException("Invalid role");
        }
        Budget += refund;
    }
}
