using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Shared read-only presentation for pre-run and in-run challenge indicators.</summary>
public static class ChallengeDisplay
{
    public static IReadOnlyList<string> VisibleIds(ChallengeSelections selections, bool inRun) =>
        inRun ? selections.Active : selections.Selected;

    public static string Describe(ChallengeRegistry registry, IEnumerable<string> ids, EntityCatalog? catalog = null)
    {
        var descriptors = registry.List().ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
        string[] visible = ids.ToArray();
        string descriptions = string.Join("\n\n", visible.Select(id =>
        {
            if (!descriptors.TryGetValue(id, out ChallengeDescriptor? descriptor)) return id;
            string incompatibilities = descriptor.KnownIncompatibilities is { Count: > 0 }
                ? "\nIncompatible with: " + string.Join(", ", descriptor.KnownIncompatibilities.Select(otherId =>
                    descriptors.TryGetValue(otherId, out var other) ? other.Name : otherId))
                : "";
            return $"<b>{descriptor.Name}{(string.IsNullOrWhiteSpace(descriptor.Author) ? "" : $" ({descriptor.Author})")}</b>\n{descriptor.Description}{incompatibilities}";
        }));
        string restrictions = ChallengeRestrictions.Describe(ChallengeRestrictions.For(visible, catalog), catalog);
        return string.IsNullOrEmpty(restrictions) ? descriptions : string.IsNullOrEmpty(descriptions) ? restrictions : descriptions + "\n\n" + restrictions;
    }
}
