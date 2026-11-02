using System.Text.Json;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Persists disabled module identifiers; omitted modules remain enabled by default.</summary>
public sealed class ModuleConfigurationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string path;

    public ModuleConfigurationStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Configuration path is required.", nameof(path));
        }

        this.path = path;
    }

    public ModuleConfiguration Load()
    {
        if (!File.Exists(path))
        {
            return new ModuleConfiguration();
        }

        using FileStream stream = File.OpenRead(path);
        ModuleConfigurationDocument? document = JsonSerializer.Deserialize<ModuleConfigurationDocument>(stream);
        if (document?.DisabledModules is null || document.DisabledModules.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("Module configuration must contain valid disabled module identifiers.");
        }

        return new ModuleConfiguration(document.DisabledModules);
    }

    public void Save(IEnumerable<string> disabledModuleIds)
    {
        ArgumentNullException.ThrowIfNull(disabledModuleIds);
        string[] ids = disabledModuleIds.ToArray();
        if (ids.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Module identifiers must not be blank.", nameof(disabledModuleIds));
        }

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = path + ".tmp";
        try
        {
            using (FileStream stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, new ModuleConfigurationDocument(ids.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray()),
                    SerializerOptions);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record ModuleConfigurationDocument(string[] DisabledModules);
}
