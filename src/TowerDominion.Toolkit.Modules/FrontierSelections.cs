using System.Text.Json;
using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Persistent desired Frontier selections; UI synchronization is a separate game adapter.</summary>
public sealed class FrontierSelections
{
    private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
    private readonly string path;
    private readonly HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase);

    public FrontierSelections(string path)
    {
        this.path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Selection path is required.", nameof(path)) : path;
        if (!File.Exists(path)) return;
        string[] ids = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid Frontier selection file.");
        if (ids.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Frontier IDs cannot be blank.");
        selected.UnionWith(ids);
    }

    public IReadOnlyCollection<string> SelectedIds => selected.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    public bool IsSelected(string id) => selected.Contains(id);

    public (int Changed, int Unchanged) Change(IEnumerable<string> ids, bool select)
    {
        ArgumentNullException.ThrowIfNull(ids);
        string[] unique = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (unique.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Frontier IDs cannot be blank.", nameof(ids));
        var next = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
        int changed = 0;
        foreach (string id in unique)
            if (select ? next.Add(id) : next.Remove(id)) changed++;
        if (changed == 0) return (0, unique.Length);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory != null) Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(next.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(), serializerOptions));
            File.Move(temporary, path, true);
            selected.Clear();
            selected.UnionWith(next);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return (changed, unique.Length - changed);
    }
}

public sealed class FrontierCommands
{
    private readonly EntityCatalog catalog;
    private readonly FrontierSelections selections;
    private readonly Func<bool> isReady;

    public FrontierCommands(EntityCatalog catalog, FrontierSelections selections, Func<bool> isReady)
    {
        this.catalog = catalog;
        this.selections = selections;
        this.isReady = isReady;
    }

    public string Execute(string command)
    {
        string[] parts = command.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        bool select = parts.Length > 0 && parts[0].Equals("select", StringComparison.OrdinalIgnoreCase);
        string usage = $"Usage: {(select ? "select" : "unselect")} frontier <\"name\",\"name\"|all>";
        if (parts.Length != 3 || !parts[1].Equals("frontier", StringComparison.OrdinalIgnoreCase)) return usage;
        if (!isReady()) return "Game catalog is not ready yet. Try again after loading.";

        string[] names;
        if (parts[2].Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
            names = catalog.List("frontiers").Select(x => x.Id).ToArray();
        else if (!TryParseNames(parts[2], out names)) return usage;

        var resolved = new List<string>();
        foreach (string name in names)
        {
            if (!catalog.TryResolve("frontiers", name, out CatalogEntity? entity))
                return $"Unknown or ambiguous Frontier modifier: {name}. No selections changed.";
            resolved.Add(entity!.Id);
        }
        var result = selections.Change(resolved, select);
        return $"{(select ? "Selected" : "Unselected")} {result.Changed} persistent Frontier modifiers; {result.Unchanged} unchanged. Changes will be applied when the Frontier screen is synchronized.";
    }

    public string ListSelected(string command)
    {
        if (!command.Trim().Equals("list selected-frontiers", StringComparison.OrdinalIgnoreCase)) return "Usage: list selected-frontiers";
        var ids = selections.SelectedIds;
        var known = catalog.List("frontiers").ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        return $"Persistent Frontier selections ({ids.Count}):" + (ids.Count == 0 ? " (none)" : Environment.NewLine + string.Join(Environment.NewLine, ids.Select(id => known.TryGetValue(id, out var entry) ? $"{entry.Name} [{id}]" : id)));
    }

    private static bool TryParseNames(string text, out string[] names)
    {
        var result = new List<string>();
        foreach (string item in text.Split(','))
        {
            string name = item.Trim();
            if (name.Length >= 2 && name[0] == '"' && name[^1] == '"') name = name[1..^1].Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Contains('"')) { names = Array.Empty<string>(); return false; }
            result.Add(name);
        }
        names = result.ToArray();
        return names.Length > 0;
    }
}
