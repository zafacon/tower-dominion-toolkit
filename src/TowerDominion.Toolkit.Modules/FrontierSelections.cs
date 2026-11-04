using System.Text.Json;

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

    /// <summary>Replace the entire desired selection with one durable write.</summary>
    public void Replace(IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        string[] requested = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (requested.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Frontier IDs cannot be blank.", nameof(ids));
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory != null) Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(requested.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(), serializerOptions));
            File.Move(temporary, path, true);
            selected.Clear();
            selected.UnionWith(requested);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

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
