namespace Domain.Services;

using Domain.Entities;
using Domain.Enums;

/// <summary>
/// Servizio di dominio stateless per algoritmi di gestione asta
/// </summary>
public static class AuctionFlow
{
    public static (bool AutoAssign, IReadOnlyList<Guid> EligibleOthers) EvaluateNomination(
        IEnumerable<LeaguePlayer> allTeams, 
        Guid nominatorId, 
        PlayerType role)
    {
        var nominatorTeam = allTeams.FirstOrDefault(t => t.Id == nominatorId);
        var eligibleTeams = allTeams.Where(t => t.Id != nominatorId && t.HasSlot(role)).ToList();
        
        // Auto-assignment se:
        // 1. Nessun altro team può fare offerte E
        // 2. Il nominatore stesso ha spazio per il giocatore
        if (eligibleTeams.Count == 0)
        {
            return nominatorTeam?.HasSlot(role) == true 
                ? (true, Array.Empty<Guid>())
                : (false, Array.Empty<Guid>()); // Nessuno può ricevere il giocatore
        }
        
        // Ready check se ci sono altri team con slot
        return (false, eligibleTeams.Select(t => t.Id).ToList());
    }

    public static (PlayerType? NextRole, int NextIndex) AdvanceUntilEligible(
        IReadOnlyList<Guid> teamOrder,
        IReadOnlyDictionary<Guid, LeaguePlayer> teams,
        PlayerType currentRole,
        int currentIndex,
        IReadOnlyList<PlayerType>? configuredOrder = null)
    {
        var roles = configuredOrder?.Count > 0
            ? configuredOrder.ToArray()
            : new[] { PlayerType.Goalkeeper, PlayerType.Defender, PlayerType.Midfielder, PlayerType.Forward };
        var currentRoleIndex = Array.IndexOf(roles, currentRole);
        
    // Debug tracing rimosso: mantenere purezza dominio. (Si può integrare un logger esterno via adapter se necessario.)
        
        // Prima prova a completare il ciclo nello stesso ruolo (logica circolare)
        // Controlla dal prossimo team fino alla fine
        for (int teamIdx = currentIndex + 1; teamIdx < teamOrder.Count; teamIdx++)
        {
            var teamId = teamOrder[teamIdx];
            if (teams.TryGetValue(teamId, out var team))
            {
                var hasSlot = team.HasSlot(currentRole);
                if (hasSlot)
                {
                    return (currentRole, teamIdx);
                }
            }
        }
        
        // Poi controlla dall'inizio fino al team corrente (completamento circolare)
        for (int teamIdx = 0; teamIdx <= currentIndex; teamIdx++)
        {
            var teamId = teamOrder[teamIdx];
            if (teams.TryGetValue(teamId, out var team))
            {
                var hasSlot = team.HasSlot(currentRole);
                if (hasSlot)
                {
                    return (currentRole, teamIdx);
                }
            }
        }
        
        // Solo dopo aver completato il ciclo del ruolo corrente, passa ai ruoli successivi
        for (int roleIdx = currentRoleIndex + 1; roleIdx < roles.Length; roleIdx++)
        {
            var nextRole = roles[roleIdx];
            for (int teamIdx = 0; teamIdx < teamOrder.Count; teamIdx++)
            {
                var teamId = teamOrder[teamIdx];
                if (teams.TryGetValue(teamId, out var team))
                {
                    var hasSlot = team.HasSlot(nextRole);
                    if (hasSlot)
                    {
                        return (nextRole, teamIdx);
                    }
                }
            }
        }
        
        // Nessun turno disponibile - asta completata
        return (null, currentIndex);
    }
}
