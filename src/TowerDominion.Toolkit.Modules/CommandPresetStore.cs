using System.Text.Json;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Domain-neutral named JSON snapshots. Validation and state replacement belong to domain adapters.</summary>
public sealed class CommandPresetStore<T>
{
    private readonly string directory;
    private readonly Func<T, string?> validate;

    public CommandPresetStore(string directory, Func<T, string?> validate)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Preset directory is required.", nameof(directory));
        this.directory = directory;
        this.validate = validate ?? throw new ArgumentNullException(nameof(validate));
    }

    private string PathFor(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name is "." or ".." ||
            name.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_' or ' ')))
            throw new ArgumentException("Invalid preset name.", nameof(name));
        return Path.Combine(directory, name + ".json");
    }

    public IReadOnlyList<string> Names() => Directory.Exists(directory)
        ? Directory.GetFiles(directory, "*.json").Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null).Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()
        : Array.Empty<string>();

    public T Read(string name)
    {
        var value = JsonSerializer.Deserialize<T>(File.ReadAllText(PathFor(name)));
        if (value is null) throw new InvalidDataException("Invalid preset payload.");
        string? error = validate(value);
        if (error is not null) throw new InvalidDataException(error);
        return value;
    }

    public void Save(string name, T snapshot)
    {
        string path = PathFor(name);
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
        string? error = validate(snapshot);
        if (error is not null) throw new ArgumentException(error, nameof(snapshot));
        Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public bool Delete(string name)
    {
        string path = PathFor(name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }
}
