namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

public class Player : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public PlayerType Role { get; private set; }
    public string TeamName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true; // false if left the league

    private Player() { }

    public static Player Create(string firstName, string lastName, PlayerType role, string teamName, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name required");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name required");
        if (string.IsNullOrWhiteSpace(teamName)) throw new DomainException("Team name required");

        return new Player
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = role,
            TeamName = teamName.Trim(),
            IsActive = isActive
        };
    }

    public void MarkInactive() => IsActive = false;
}
