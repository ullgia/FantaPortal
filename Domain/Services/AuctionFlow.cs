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
        int currentIndex)
    {
        var roles = new[] { PlayerType.Goalkeeper, PlayerType.Defender, PlayerType.Midfielder, PlayerType.Forward };
        var currentRoleIndex = Array.IndexOf(roles, currentRole);
        
        Console.WriteLine($"=== AdvanceUntilEligible DEBUG ===");
        Console.WriteLine($"Input: currentRole={currentRole}, currentIndex={currentIndex}, teamCount={teamOrder.Count}");
        
        // Prima prova a completare il ciclo nello stesso ruolo (logica circolare)
        // Controlla dal prossimo team fino alla fine
        Console.WriteLine($"Step 1: Checking teams {currentIndex + 1} to {teamOrder.Count - 1} for role {currentRole}");
        for (int teamIdx = currentIndex + 1; teamIdx < teamOrder.Count; teamIdx++)
        {
            var teamId = teamOrder[teamIdx];
            if (teams.TryGetValue(teamId, out var team))
            {
                var hasSlot = team.HasSlot(currentRole);
                Console.WriteLine($"  Team {teamIdx}: hasSlot({currentRole})={hasSlot}");
                if (hasSlot)
                {
                    Console.WriteLine($"  -> RETURN ({currentRole}, {teamIdx})");
                    return (currentRole, teamIdx);
                }
            }
        }
        
        // Poi controlla dall'inizio fino al team corrente (completamento circolare)
        Console.WriteLine($"Step 2: Checking teams 0 to {currentIndex} for role {currentRole}");
        for (int teamIdx = 0; teamIdx <= currentIndex; teamIdx++)
        {
            var teamId = teamOrder[teamIdx];
            if (teams.TryGetValue(teamId, out var team))
            {
                var hasSlot = team.HasSlot(currentRole);
                Console.WriteLine($"  Team {teamIdx}: hasSlot({currentRole})={hasSlot}");
                if (hasSlot)
                {
                    Console.WriteLine($"  -> RETURN ({currentRole}, {teamIdx})");
                    return (currentRole, teamIdx);
                }
            }
        }
        
        // Solo dopo aver completato il ciclo del ruolo corrente, passa ai ruoli successivi
        Console.WriteLine($"Step 3: Advancing to next roles starting from index {currentRoleIndex + 1}");
        for (int roleIdx = currentRoleIndex + 1; roleIdx < roles.Length; roleIdx++)
        {
            var nextRole = roles[roleIdx];
            Console.WriteLine($"  Checking role {nextRole}");
            for (int teamIdx = 0; teamIdx < teamOrder.Count; teamIdx++)
            {
                var teamId = teamOrder[teamIdx];
                if (teams.TryGetValue(teamId, out var team))
                {
                    var hasSlot = team.HasSlot(nextRole);
                    Console.WriteLine($"    Team {teamIdx}: hasSlot({nextRole})={hasSlot}");
                    if (hasSlot)
                    {
                        Console.WriteLine($"    -> RETURN ({nextRole}, {teamIdx})");
                        return (nextRole, teamIdx);
                    }
                }
            }
        }
        
        // Nessun turno disponibile - asta completata
        Console.WriteLine($"-> RETURN (null, {currentIndex})");
        return (null, currentIndex);
    }
}
