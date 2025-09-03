namespace Domain.Services;

using Domain.Entities;
using Domain.Enums;

public static class AuctionFlow
{
    public sealed record NominationResult(bool AutoAssign, int AssignPrice, IReadOnlyList<Guid> EligibleOthers);

    public static IReadOnlyList<Guid> GetEligibleTeamsForRole(IEnumerable<Team> teams, RoleType role)
        => teams.Where(t => t.HasSlot(role)).Select(t => t.Id).ToList();

    // Returns whether to auto-assign at price 1 or wait for readiness of eligible others
    public static NominationResult EvaluateNomination(
        IEnumerable<Team> teams,
        Guid nominatorTeamId,
        RoleType role)
    {
        var eligible = GetEligibleTeamsForRole(teams, role);
        var others = eligible.Where(id => id != nominatorTeamId).ToList();
        var autoAssign = others.Count == 0; // "se nessuno deve essere pronto" => auto-assegna a 1
        return new NominationResult(autoAssign, autoAssign ? 1 : 0, others);
    }

    // Given an order and a start index, find next team with slot for role; if none in full cycle, returns -1
    public static int FindNextEligibleIndex(IReadOnlyList<Guid> order, int startIndex, IReadOnlyDictionary<Guid, Team> teams, RoleType role)
    {
        if (order.Count == 0) return -1;
        for (int i = 0; i < order.Count; i++)
        {
            int idx = (startIndex + i) % order.Count;
            var teamId = order[idx];
            if (teams.TryGetValue(teamId, out var team) && team.HasSlot(role))
                return idx;
        }
        return -1;
    }

    // Advance role order: P -> D -> C -> A -> null (done)
    public static RoleType? NextRole(RoleType current)
        => current switch
        {
            RoleType.P => RoleType.D,
            RoleType.D => RoleType.C,
            RoleType.C => RoleType.A,
            RoleType.A => null,
            _ => null
        };

    // If there is no eligible team for current role across the entire order, move to next role and try again.
    public static (RoleType? role, int index) AdvanceUntilEligible(
        IReadOnlyList<Guid> order,
        IReadOnlyDictionary<Guid, Team> teams,
        RoleType startRole,
        int startIndex)
    {
        RoleType? role = startRole;
        int idx = startIndex;
        while (role is not null)
        {
            var found = FindNextEligibleIndex(order, idx, teams, role.Value);
            if (found >= 0)
                return (role, found);
            role = NextRole(role.Value);
            idx = 0; // restart from beginning for new role
        }
        return (null, -1); // No roles left
    }
}
