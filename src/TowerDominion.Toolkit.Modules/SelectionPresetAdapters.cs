namespace TowerDominion.Toolkit.Modules;

/// <summary>Domain-specific snapshot adapters over the shared named preset store.</summary>
public sealed class SelectionPresetAdapters
{
    private readonly CommandPresetStore<Dictionary<string, string[]>> banStore;
    private readonly CommandPresetStore<string[]> frontierStore;
    private readonly BanSelections bans;
    private readonly FrontierSelections frontiers;
    private readonly PinSelections? pins;
    private readonly CommandPresetStore<Dictionary<string, string[]>> pinStore;

    public SelectionPresetAdapters(string directory, BanSelections bans, FrontierSelections frontiers, PinSelections? pins = null)
    {
        this.bans = bans ?? throw new ArgumentNullException(nameof(bans));
        this.frontiers = frontiers ?? throw new ArgumentNullException(nameof(frontiers));
        this.pins = pins;
        pinStore = new(Path.Combine(directory, "pins"), ValidatePins);
        banStore = new(Path.Combine(directory, "bans"), ValidateBans);
        frontierStore = new(Path.Combine(directory, "frontiers"), ValidateFrontiers);
    }

    public IReadOnlyDictionary<string, string[]> SnapshotBans() =>
        BanSelections.Categories.ToDictionary(category => category,
            category => bans.List(category).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray());

    public IReadOnlyDictionary<string, string[]> SnapshotPins() =>
        PinSelections.Categories.ToDictionary(category => category,
            category => pins?.List(category).ToArray() ?? Array.Empty<string>());

    public string[] SnapshotFrontiers() =>
        frontiers.SelectedIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<string> Names(string domain) => domain switch
    {
        "bans" => banStore.Names(),
        "pins" => pinStore.Names(),
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
            case "pins":
                if (pins is null) throw new InvalidOperationException("Pins unavailable.");
                pinStore.Save(name, SnapshotPins().ToDictionary(x => x.Key, x => x.Value));
                break;
            case "frontiers": frontierStore.Save(name, frontiers.SelectedIds.ToArray()); break;
            default: throw new ArgumentException("Unsupported preset domain.", nameof(domain));
        }
    }

    public string Inspect(string domain, string name) => domain switch
    {
        "bans" => string.Join("; ", banStore.Read(name).OrderBy(x => x.Key)
            .Select(x => $"{x.Key}: {string.Join(", ", x.Value)}")),
        "pins" => string.Join("; ", pinStore.Read(name).OrderBy(x => x.Key)
            .Select(x => $"{x.Key}: {string.Join(", ", x.Value)}")),
        "frontiers" => string.Join(", ", frontierStore.Read(name)),
        _ => throw new ArgumentException("Unsupported preset domain.", nameof(domain))
    };

    public void Load(string domain, string name)
    {
        switch (domain)
        {
            case "bans":
                var incoming = banStore.Read(name);
                if (pins is not null)
                    foreach (var (category, ids) in incoming)
                        if (PinSelections.Categories.Contains(category, StringComparer.OrdinalIgnoreCase) &&
                            ids.Any(id => pins.IsPinned(category, id)))
                            throw new InvalidOperationException($"Cannot load bans: {category} contains a pinned encounter.");
                bans.Replace(incoming);
                break;
            case "pins":
                if (pins is null) throw new InvalidOperationException("Pins unavailable.");
                var next = pinStore.Read(name);
                foreach (var (category, ids) in next)
                    if (ids.Any(id => bans.IsBanned(category, id)))
                        throw new InvalidOperationException($"Cannot load pins: {category} contains a banned encounter.");
                pins.Replace(next);
                break;
            case "frontiers": frontiers.Replace(frontierStore.Read(name)); break;
            default: throw new ArgumentException("Unsupported preset domain.", nameof(domain));
        }
    }

    public bool Delete(string domain, string name) => domain switch
    {
        "bans" => banStore.Delete(name),
        "pins" => pinStore.Delete(name),
        "frontiers" => frontierStore.Delete(name),
        _ => throw new ArgumentException("Unsupported preset domain.", nameof(domain))
    };

    private static string? ValidateBans(Dictionary<string, string[]> snapshot) =>
        snapshot.Keys.Any(key => !BanSelections.Categories.Contains(key, StringComparer.OrdinalIgnoreCase)) ||
        snapshot.Values.Any(ids => ids is null || ids.Any(string.IsNullOrWhiteSpace))
            ? "Invalid ban preset." : null;

    private static string? ValidatePins(Dictionary<string, string[]> snapshot) =>
        snapshot.Keys.Any(key => !PinSelections.Categories.Contains(key, StringComparer.OrdinalIgnoreCase)) ||
        snapshot.Values.Any(ids => ids is null || ids.Any(string.IsNullOrWhiteSpace))
            ? "Invalid pin preset." : null;

    private static string? ValidateFrontiers(string[] ids) =>
        ids.Any(string.IsNullOrWhiteSpace) ? "Invalid Frontier preset." : null;
}
