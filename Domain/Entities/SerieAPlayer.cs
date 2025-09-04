namespace Domain.Entities;

using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;


public class SerieAPlayer : AggregateRoot<int>
{
    public string Role { get; private set; } = default!; // P, D, C, A
    public string RoleExtended { get; private set; } = default!; // Por, Dd, Ds, E, M, W, C, T, A, Pc
    public string Name { get; private set; } = default!;
    public string Team { get; private set; } = default!;

    public decimal QuotationA { get; private set; }
    public decimal QuotationI { get; private set; }
    public decimal Difference => QuotationA - QuotationI;

    public decimal? QuotationAMantra { get; private set; }
    public decimal? QuotationIMantra { get; private set; }
    public decimal? DifferenceMantra => (QuotationAMantra ?? 0) - (QuotationIMantra ?? 0);

    public int FVM { get; private set; }
    public int? FVMMantra { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
    public string ImageUrl => $"https://content.fantacalcio.it/web/campioncini/20/card/{Id}.png?v=331,0,0.0,0.0";

    private readonly List<PlayerOwnership> _ownerships = new();
    public IReadOnlyCollection<PlayerOwnership> Ownerships => _ownerships.AsReadOnly();

    [NotMapped]
    public PlayerType PlayerType
    {
        get
        {
            return Role.Trim() switch
            {
                "P" => PlayerType.Goalkeeper,
                "D" => PlayerType.Defender,
                "C" => PlayerType.Midfielder,
                "M" => PlayerType.Midfielder,
                "A" => PlayerType.Forward,
                "F" => PlayerType.Forward,
                _ => throw new DomainException("Invalid player role")
            };
        }
    }

    private SerieAPlayer() { }

    public static SerieAPlayer Create(int id, string role, string roleExtended, string name, string team, decimal qtA, decimal qtI, int fvm)
    {
        if (string.IsNullOrWhiteSpace(role)) throw new DomainException("Role is required");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required");
        if (string.IsNullOrWhiteSpace(team)) throw new DomainException("Team is required");

        return new SerieAPlayer
        {
            Id = id,
            Role = role.Trim(),
            RoleExtended = roleExtended?.Trim() ?? role.Trim(),
            Name = name.Trim(),
            Team = team.Trim(),
            QuotationA = qtA,
            QuotationI = qtI,
            FVM = fvm,
            LastUpdated = DateTime.UtcNow
        };
    }

    public void UpdateFromImport(string role, string roleExtended, string name, string team, decimal qtA, decimal qtI, int fvm)
    {
        Role = role?.Trim() ?? Role;
        RoleExtended = roleExtended?.Trim() ?? RoleExtended;
        Name = name?.Trim() ?? Name;
        Team = team?.Trim() ?? Team;
        QuotationA = qtA;
        QuotationI = qtI;
        FVM = fvm;
        IsActive = true;
        LastUpdated = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsActive = false;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateQuotations(decimal qtA, decimal qtI, decimal? qtAM = null, decimal? qtIM = null)
    {
        if (qtA < 0 || qtI < 0) throw new DomainException("Quotations must be non-negative");
        QuotationA = qtA;
        QuotationI = qtI;
        QuotationAMantra = qtAM;
        QuotationIMantra = qtIM;
        LastUpdated = DateTime.UtcNow;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        LastUpdated = DateTime.UtcNow;
    }
}
