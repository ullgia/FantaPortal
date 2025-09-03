namespace Domain.Contracts;

public interface ITeamValidator
{
    Task<bool> IsTeamNameUniqueAsync(Guid leagueId, string name, CancellationToken ct = default);
}
