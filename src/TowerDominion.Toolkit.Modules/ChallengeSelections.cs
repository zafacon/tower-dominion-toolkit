using System.Text.Json;
using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Persistent pre-run challenge selection and immutable run snapshot.</summary>
public sealed class ChallengeSelections
{
    private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
    private readonly string path;
    private readonly ChallengeRegistry registry;
    private readonly HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase);
    private string[]? active;

    public ChallengeSelections(string path, ChallengeRegistry registry)
    {
        this.path = path;
        this.registry = registry;
        if (!File.Exists(path)) return;
        string[] loaded = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid challenge selection file.");
        if (loaded.Any(id => string.IsNullOrWhiteSpace(id) || !registry.TryGet(id, out _)))
            throw new InvalidDataException("Challenge selection contains an unknown identifier.");
        selected.UnionWith(loaded);
    }

    public bool RunActive => active is not null;
    public IReadOnlyList<string> Selected => selected.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
    public IReadOnlyList<string> Active => active is null ? Array.Empty<string>() : Array.AsReadOnly(active);

    public void Change(IEnumerable<string> ids, bool enable)
    {
        if (RunActive) throw new InvalidOperationException("Challenges cannot be changed during a run.");
        string[] requested = ids.ToArray();
        if (requested.Any(id => string.IsNullOrWhiteSpace(id) || !registry.TryGet(id, out _)))
            throw new ArgumentException("Unknown challenge identifier.", nameof(ids));
        HashSet<string> next = new(selected, StringComparer.OrdinalIgnoreCase);
        foreach (string id in requested)
        {
            if (enable) next.Add(id);
            else next.Remove(id);
        }
        Save(next);
        selected.Clear();
        selected.UnionWith(next);
    }

    public void Replace(IEnumerable<string> ids)
    {
        if (RunActive) throw new InvalidOperationException("Challenges cannot be changed during a run.");
        ArgumentNullException.ThrowIfNull(ids);
        string[] requested = ids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (requested.Any(id => string.IsNullOrWhiteSpace(id) || !registry.TryGet(id, out _)))
            throw new ArgumentException("Unknown challenge identifier.", nameof(ids));
        Save(requested);
        selected.Clear();
        selected.UnionWith(requested);
    }

    public void Clear()
    {
        if (RunActive) throw new InvalidOperationException("Challenges cannot be changed during a run.");
        Save(Array.Empty<string>());
        selected.Clear();
    }

    public void StartRun()
    {
        if (RunActive) throw new InvalidOperationException("A challenge run is already active.");
        active = Selected.ToArray();
    }

    public void EndRun() => active = null;

    private void Save(IEnumerable<string> ids)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(ids.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(), serializerOptions));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
