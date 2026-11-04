using System.Text.Json;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Persistent category-qualified IDs; game-side enforcement is a separate adapter.</summary>
public sealed class BanSelections
{
    private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
    public static readonly string[] Categories = { "foes", "waves", "blueprints", "doctrines" };
    private readonly string path;
    private readonly bool persist;
    private readonly Dictionary<string, HashSet<string>> banned = Categories.ToDictionary(x => x, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

    public BanSelections(string path, bool persist = true)
    {
        this.path = path;
        this.persist = persist;
        if (!persist || !File.Exists(path)) return;
        var loaded = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid ban file.");
        foreach (var (category, ids) in loaded)
        {
            if (!banned.TryGetValue(category, out var target) || ids == null || ids.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Invalid ban category or ID.");
            target.UnionWith(ids);
        }
    }

    public bool IsBanned(string category, string id) => banned.TryGetValue(category, out var ids) && ids.Contains(id);
    public IReadOnlyCollection<string> List(string category) => banned.TryGetValue(category, out var ids) ? ids.OrderBy(x => x).ToArray() : Array.Empty<string>();

    public void Replace(IReadOnlyDictionary<string, string[]> replacement)
    {
        if (replacement.Keys.Any(x => !Categories.Contains(x, StringComparer.OrdinalIgnoreCase)) || replacement.Values.Any(x => x == null || x.Any(string.IsNullOrWhiteSpace)))
            throw new ArgumentException("Invalid ban replacement.");
        var next = Categories.ToDictionary(x => x, x => replacement.TryGetValue(x, out var ids) ? ids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() : Array.Empty<string>());
        if (!persist)
        {
            foreach (string key in Categories) { banned[key].Clear(); banned[key].UnionWith(next[key]); }
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(next, serializerOptions));
            File.Move(temporary, path, true);
            foreach (string key in Categories) { banned[key].Clear(); banned[key].UnionWith(next[key]); }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public (int Changed, int Unchanged) Change(string category, IEnumerable<string> ids, bool add)
    {
        if (!banned.ContainsKey(category)) throw new ArgumentException("Invalid ban category.", nameof(category));
        string[] unique = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (unique.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Blank ban ID.", nameof(ids));
        var next = banned.ToDictionary(x => x.Key, x => new HashSet<string>(x.Value, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        int changed = 0;
        foreach (string id in unique) if (add ? next[category].Add(id) : next[category].Remove(id)) changed++;
        if (changed == 0) return (0, unique.Length);
        if (!persist)
        {
            foreach (string key in Categories) { banned[key].Clear(); banned[key].UnionWith(next[key]); }
            return (changed, unique.Length - changed);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(next.ToDictionary(x => x.Key, x => x.Value.OrderBy(id => id).ToArray()), serializerOptions));
            File.Move(temporary, path, true);
            foreach (string key in Categories) { banned[key].Clear(); banned[key].UnionWith(next[key]); }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return (changed, unique.Length - changed);
    }
}
