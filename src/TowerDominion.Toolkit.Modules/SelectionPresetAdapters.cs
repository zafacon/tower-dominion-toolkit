namespace TowerDominion.Toolkit.Modules;

/// <summary>Domain-specific snapshot adapters over the shared named preset store.</summary>
public sealed class SelectionPresetAdapters
{
    private readonly CommandPresetStore<Dictionary<string, string[]>> banStore;
    private readonly CommandPresetStore<string[]> frontierStore;
    private readonly BanSelections bans;
    private readonly FrontierSelections frontiers;

    public SelectionPresetAdapters(string directory, BanSelections bans, FrontierSelections frontiers)
    {
        this.bans = bans ?? throw new ArgumentNullException(nameof(bans));
        this.frontiers = frontiers ?? throw new ArgumentNullException(nameof(frontiers));
        banStore = new(Path.Combine(directory, "bans"), ValidateBans);
        frontierStore = new(Path.Combine(directory, "frontiers"), ValidateFrontiers);
    }

    public IReadOnlyDictionary<string, string[]> SnapshotBans() =>
        BanSelections.Categories.ToDictionary(category => category,
            category => bans.List(category).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray());

    public string[] SnapshotFrontiers() =>
        frontiers.SelectedIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<string> Names(string domain) => domain switch
    {
        "bans" => banStore.Names(),
        "frontiers" => frontierStore.Names(),
        _ => throw new ArgumentException("Unsupported preset domain.", nameof(domain))
    };

    public void Save(string domain, string name)
    {
        switch (domain)
        {
            case "bans":
                banStore.Save(name, BanSelections.Categories.ToDictionary(category => category,
                    category => bans.List(category).ToArray()));
                break;
            case "frontiers": frontierStore.Save(name, frontiers.SelectedIds.ToArray()); break;
            default: throw new ArgumentException("Unsupported preset domain.", nameof(domain));
        }
    }

    public string Inspect(string domain, string name) => domain switch
    {
        "bans" => string.Join("; ", banStore.Read(name).OrderBy(x => x.Key)
            .Select(x => $"{x.Key}: {string.Join(", ", x.Value)}")),
        "frontiers" => string.Join(", ", frontierStore.Read(name)),
        _ => throw new ArgumentException("Unsupported preset domain.", nameof(domain))
    };

    public void Load(string domain, string name)
    {
        switch (domain)
        {
            case "bans": bans.Replace(banStore.Read(name)); break;
            case "frontiers": frontiers.Replace(frontierStore.Read(name)); break;
            default: throw new ArgumentException("Unsupported preset domain.", nameof(domain));
        }
    }

    public bool Delete(string domain, string name) => domain switch
    {
        "bans" => banStore.Delete(name),
        "frontiers" => frontierStore.Delete(name),
        _ => throw new ArgumentException("Unsupported preset domain.", nameof(domain))
    };

    private static string? ValidateBans(Dictionary<string, string[]> snapshot) =>
        snapshot.Keys.Any(key => !BanSelections.Categories.Contains(key, StringComparer.OrdinalIgnoreCase)) ||
        snapshot.Values.Any(ids => ids is null || ids.Any(string.IsNullOrWhiteSpace))
            ? "Invalid ban preset." : null;

    private static string? ValidateFrontiers(string[] ids) =>
        ids.Any(string.IsNullOrWhiteSpace) ? "Invalid Frontier preset." : null;
}
