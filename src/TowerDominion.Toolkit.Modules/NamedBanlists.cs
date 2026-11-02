using System.Text.Json;
using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Named snapshots of active bans. Stable IDs remain internal.</summary>
public sealed class NamedBanlists
{
    private static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
    private readonly string directory;
    private readonly BanSelections active;
    private readonly EntityCatalog catalog;
    public NamedBanlists(string directory, BanSelections active, EntityCatalog catalog)
    { this.directory = directory; this.active = active; this.catalog = catalog; }

    private string PathFor(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.Length > 100 || name.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_' or ' ')))
            throw new ArgumentException("Banlist name must contain only letters, numbers, spaces, hyphens, or underscores.");
        return Path.Combine(directory, name + ".json");
    }
    public IReadOnlyList<string> Names() => Directory.Exists(directory)
        ? Directory.GetFiles(directory, "*.json").Select(Path.GetFileNameWithoutExtension).Where(x => x != null).Select(x => x!).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
        : Array.Empty<string>();
    public Dictionary<string, string[]> Read(string name)
    {
        var loaded = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(PathFor(name))) ?? throw new InvalidDataException("Invalid banlist.");
        if (loaded.Keys.Any(x => !BanSelections.Categories.Contains(x, StringComparer.OrdinalIgnoreCase)) || loaded.Values.Any(x => x == null || x.Any(string.IsNullOrWhiteSpace)))
            throw new InvalidDataException("Invalid banlist entries.");
        return loaded;
    }
    public void Save(string name)
    {
        string path = PathFor(name);
        Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(BanSelections.Categories.ToDictionary(x => x, x => active.List(x).ToArray()), serializerOptions));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public int Load(string name)
    {
        var data = Read(name);
        var replacement = BanSelections.Categories.ToDictionary(x => x, x => data.TryGetValue(x, out var ids) ? ids : Array.Empty<string>());
        active.Replace(replacement);
        return replacement.Sum(x => x.Value.Length);
    }
    public int Unload(string name)
    {
        var data = Read(name);
        return data.Sum(x => active.Change(x.Key, x.Value, false).Changed);
    }
    public bool Delete(string name)
    {
        string path = PathFor(name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }
    public string List(string name)
    {
        var entries = Read(name).SelectMany(x => x.Value.Select(id => x.Key + ": " + (catalog.List(x.Key).FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id))).OrderBy(x => x).ToArray();
        return $"Banlist '{name}' ({entries.Length}):" + (entries.Length == 0 ? " (empty)" : Environment.NewLine + string.Join(Environment.NewLine, entries));
    }
    public string Execute(string command)
    {
        string trimmed = command.Trim();
        if (trimmed.StartsWith("save banned as ", StringComparison.OrdinalIgnoreCase)) { string name = trimmed[15..].Trim(); Save(name); return $"Saved banlist '{name}'."; }
        string[] parts = trimmed.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && parts[0].Equals("list", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("banlists", StringComparison.OrdinalIgnoreCase))
            return "Saved banlists: " + string.Join(", ", Names());
        if (parts.Length != 3 || !parts[1].Equals("banlist", StringComparison.OrdinalIgnoreCase)) return "Usage: save banned as <name> | load/unload/delete/list banlist <name> | list banlists";
        string nameArg = parts[2].Trim();
        try
        {
            return parts[0].ToLowerInvariant() switch
            {
                "load" => $"Loaded banlist '{nameArg}' ({Load(nameArg)} entries).",
                "unload" => $"Unloaded banlist '{nameArg}' ({Unload(nameArg)} entries removed).",
                "delete" => Delete(nameArg) ? $"Deleted banlist '{nameArg}'." : $"No banlist named '{nameArg}'.",
                "list" => List(nameArg),
                _ => "Unknown banlist command."
            };
        }
        catch (FileNotFoundException) { return $"No banlist named '{nameArg}'."; }
        catch (DirectoryNotFoundException) { return $"No banlist named '{nameArg}'."; }
        catch (ArgumentException ex) { return ex.Message; }
    }
}
