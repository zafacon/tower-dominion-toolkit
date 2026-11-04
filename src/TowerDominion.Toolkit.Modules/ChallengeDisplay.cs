using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Shared read-only presentation for pre-run and in-run challenge indicators.</summary>
public static class ChallengeDisplay
{
    public static IReadOnlyList<string> VisibleIds(ChallengeSelections selections, bool inRun) =>
        inRun ? selections.Active : selections.Selected;

    public static string Describe(ChallengeRegistry registry, IEnumerable<string> ids)
    {
        var descriptions = registry.List().ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);
        return string.Join("\n\n", ids.Select(id =>
        {
            if (!descriptions.TryGetValue(id, out ChallengeDescriptor? descriptor)) return id;
            return $"<b>{descriptor.Name}{(string.IsNullOrWhiteSpace(descriptor.Author) ? "" : $" ({descriptor.Author})")}</b>\n{descriptor.Description}";
        }));
    }
}
