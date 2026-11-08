namespace TowerDominion.Toolkit.Modules;

/// <summary>Session-only pinned encounter IDs; persistence is exclusively through named presets.</summary>
public sealed class PinSelections
{
    public static readonly string[] Categories = { "foes", "waves" };
    private readonly Dictionary<string, HashSet<string>> selected = Categories.ToDictionary(
        x => x, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

    public bool IsPinned(string domain, string id) => selected.TryGetValue(domain, out var ids) && ids.Contains(id);
    public IReadOnlyCollection<string> List(string domain) => selected.TryGetValue(domain, out var ids)
        ? ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray() : Array.Empty<string>();

    public void Replace(IReadOnlyDictionary<string, string[]> replacement)
    {
        if (replacement.Keys.Any(x => !Categories.Contains(x, StringComparer.OrdinalIgnoreCase)) ||
            replacement.Values.Any(x => x is null || x.Any(string.IsNullOrWhiteSpace)))
            throw new ArgumentException("Invalid pin replacement.");
        foreach (string domain in Categories)
        {
            selected[domain].Clear();
            if (replacement.TryGetValue(domain, out var ids)) selected[domain].UnionWith(ids);
        }
    }

    public (int Changed, int Unchanged) Change(string domain, IEnumerable<string> ids, bool add)
    {
        if (!selected.TryGetValue(domain, out var current)) throw new ArgumentException("Invalid pin domain.");
        var unique = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (unique.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Blank pin ID.");
        int changed = 0;
        foreach (string id in unique) if (add ? current.Add(id) : current.Remove(id)) changed++;
        return (changed, unique.Length - changed);
    }
}
