using System.Text.Json;

namespace TowerDominion.Toolkit.Modules;

public sealed record GameplayRuleDescriptor(string Id, string Name, string Description);

/// <summary>Persistent requested state; gameplay patches must separately consult effective run state.</summary>
public sealed class GameplayRules
{
    public static readonly IReadOnlyList<GameplayRuleDescriptor> Catalog = new[]
    {
        new GameplayRuleDescriptor("rear-guard-longest-path", "Rear Guard Longest Path", "Rear Guard foes use the single longest path."),
        new GameplayRuleDescriptor("bryta-nest-reroll", "Bryta Nest Reroll", "Allow rerolling Bryta nests."),
        new GameplayRuleDescriptor("no-discovery", "No Discovery", "Prevent random neutral discoveries and exclude doctrines that increase Discovery or depend on neutral buildings. Mutually exclusive with Full Discovery."),
        new GameplayRuleDescriptor("full-discovery", "Full Discovery", "Guarantee neutral discoveries on eligible tiles and exclude doctrines and random neutral buildings whose sole benefit is Discovery. Mutually exclusive with No Discovery."),
        new GameplayRuleDescriptor("no-duplicate-rewards", "No Duplicate Rewards", "Prevent the same random blueprint or doctrine from appearing more than once within a single reward-choice bundle."),
        new GameplayRuleDescriptor("abundant-tiles", "Abundant Tiles", "Make every round a tile-placement round; native nest placements remain the single placement for their round."),
        new GameplayRuleDescriptor("neutral-tile-preview", "Neutral Tile Preview", "Show the neutral at its exact eventual tile position while placing a zone.")
    };

    private readonly string path;
    private readonly HashSet<string> enabled = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public GameplayRules(string path)
    {
        this.path = path;
        if (File.Exists(path)) ReplaceInMemory(Validate(JsonSerializer.Deserialize<string[]>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Invalid rules configuration.")));
    }
    public IReadOnlyList<string> Enabled => enabled.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    public bool IsEnabled(string id) => enabled.Contains(id);
    public static string[] Validate(IEnumerable<string> ids)
    {
        if (ids is null) throw new ArgumentNullException(nameof(ids));
        string[] result = ids.ToArray();
        if (result.Any(id => string.IsNullOrWhiteSpace(id) || !Catalog.Any(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Unknown gameplay rule identifier.");
        if (result.Contains("no-discovery", StringComparer.OrdinalIgnoreCase) && result.Contains("full-discovery", StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("No Discovery and Full Discovery are mutually exclusive.");
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    public void Change(IEnumerable<string> ids, bool turnOn)
    {
        string[] requested = turnOn ? Validate(ids) : ids.SelectMany(id => Validate(new[] { id })).ToArray();
        HashSet<string> next = new(enabled, StringComparer.OrdinalIgnoreCase);
        if (turnOn && requested.Contains("no-discovery", StringComparer.OrdinalIgnoreCase)) next.Remove("full-discovery");
        if (turnOn && requested.Contains("full-discovery", StringComparer.OrdinalIgnoreCase)) next.Remove("no-discovery");
        foreach (string id in requested) { if (turnOn) next.Add(id); else next.Remove(id); }
        Replace(next);
    }
    public void Replace(IEnumerable<string> ids)
    {
        string[] next = Validate(ids);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string tmp = path + ".tmp";
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(next.OrderBy(x => x, StringComparer.OrdinalIgnoreCase), Options));
            File.Move(tmp, path, true);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
        ReplaceInMemory(next);
    }
    private void ReplaceInMemory(IEnumerable<string> ids) { enabled.Clear(); enabled.UnionWith(ids); }
}
