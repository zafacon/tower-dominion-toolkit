
namespace TowerDominion.Toolkit.Modules;

/// <summary>Challenge-owned restrictions, independent of manually configured bans.</summary>
public sealed record ChallengeRestrictionSet(
    IReadOnlyDictionary<string, IReadOnlyList<string>> Bans,
    bool NoSelling,
    bool NoCancelling)
{
    public bool IsBanned(string category, string id) =>
        Bans.TryGetValue(category, out var ids) && ids.Contains(id, StringComparer.OrdinalIgnoreCase);
}

public static class ChallengeRestrictions
{
    // Resolve localized blueprint names against the live catalog; never guess native IDs.
    public static ChallengeRestrictionSet For(IEnumerable<string> challengeIds, TowerDominion.Toolkit.Model.EntityCatalog? catalog = null)
    {
        var selected = new HashSet<string>(challengeIds, StringComparer.OrdinalIgnoreCase);
        bool removal = selected.Contains(ChainOfCommandRule.Id) || selected.Contains(ForcedDeploymentRule.Id);
        var bans = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (catalog != null)
        {
            var targets = new List<string>();
            foreach (string name in new[] { "Mission control", "Scout Hideout" })
            {
                bool required = name == "Mission control"
                    ? selected.Contains(ForcedDeploymentRule.Id) || selected.Contains(CheapestTowerOnlyRule.Id)
                    : selected.Contains(CheapestTowerOnlyRule.Id);
                if (!required || !catalog.TryResolve("blueprints", name, out var entity) || entity == null) continue;
                if (!targets.Contains(entity.Id, StringComparer.OrdinalIgnoreCase)) targets.Add(entity.Id);
            }
            if (targets.Count > 0) bans["blueprints"] = targets;
        }
        return new ChallengeRestrictionSet(bans, removal, removal);
    }

    public static string Describe(ChallengeRestrictionSet restrictions, TowerDominion.Toolkit.Model.EntityCatalog? catalog = null)
    {
        var lines = new List<string>();
        foreach (var category in restrictions.Bans.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var targets = category.Value.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            if (targets.Length > 0)
            {
                var labels = targets.Select(id => catalog?.List(category.Key)
                    .FirstOrDefault(entity => entity.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id);
                lines.Add($"Banned {category.Key}: {string.Join(", ", labels)}");
            }
        }
        if (restrictions.NoSelling) lines.Add("No Selling");
        if (restrictions.NoCancelling) lines.Add("No Cancelling");
        return lines.Count == 0 ? "" : "<b>Combined Restrictions</b>\n" + string.Join("\n", lines);
    }
}
