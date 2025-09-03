namespace Domain.Services;

using Domain.Entities;
using Domain.Enums;

/// <summary>
/// Servizio di dominio stateless per algoritmi di gestione asta
/// </summary>
public static class AuctionFlow
{
    public static (bool AutoAssign, IReadOnlyList<Guid> EligibleOthers) EvaluateNomination(
        IEnumerable<Team> allTeams, 
        Guid nominatorId, 
        RoleType role)
    {
        var eligibleTeams = allTeams.Where(t => t.Id != nominatorId && t.HasSlot(role)).ToList();
        
        // Se nessuno può fare offerte, assegnazione automatica
        return eligibleTeams.Count == 0 
            ? (true, Array.Empty<Guid>())
            : (false, eligibleTeams.Select(t => t.Id).ToList());
    }

    public static (RoleType? NextRole, int NextIndex) AdvanceUntilEligible(
        IReadOnlyList<Guid> teamOrder,
        IReadOnlyDictionary<Guid, Team> teams,
        RoleType currentRole,
        int currentIndex)
    {
        var roles = new[] { RoleType.P, RoleType.D, RoleType.C, RoleType.A };
        var currentRoleIndex = Array.IndexOf(roles, currentRole);
        
        // Prova il prossimo team nello stesso ruolo
        for (int teamIdx = currentIndex + 1; teamIdx < teamOrder.Count; teamIdx++)
        {
            var teamId = teamOrder[teamIdx];
            if (teams.TryGetValue(teamId, out var team) && team.HasSlot(currentRole))
            {
                return (currentRole, teamIdx);
            }
        }
        
        // Prova i ruoli successivi
        for (int roleIdx = currentRoleIndex + 1; roleIdx < roles.Length; roleIdx++)
        {
            var nextRole = roles[roleIdx];
            for (int teamIdx = 0; teamIdx < teamOrder.Count; teamIdx++)
            {
                var teamId = teamOrder[teamIdx];
                if (teams.TryGetValue(teamId, out var team) && team.HasSlot(nextRole))
                {
                    return (nextRole, teamIdx);
                }
            }
        }
        
        // Nessun turno disponibile - asta completata
        return (null, currentIndex);
    }
}
