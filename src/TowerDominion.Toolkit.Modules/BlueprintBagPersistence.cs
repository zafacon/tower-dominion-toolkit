using System.Security.Cryptography;
using System.Text.Json;

namespace TowerDominion.Toolkit.Modules;

/// <summary>QoL metadata only; never writes native progress or inventory.</summary>
public sealed class BlueprintBagPersistence
{
    private sealed record Snapshot(int Version, string SaveHash, string[] Ids);
    private readonly string configPath;
    private readonly string nativePath;
    public string SidecarPath => nativePath + ".tdtoolkit-qol.json";
    public bool Enabled { get; private set; }

    public BlueprintBagPersistence(string configPath, string nativePath)
    {
        this.configPath = configPath;
        this.nativePath = nativePath;
        Enabled = File.Exists(configPath) && JsonSerializer.Deserialize<bool>(File.ReadAllText(configPath));
    }

    public void SetEnabled(bool enabled)
    {
        AtomicWrite(configPath, JsonSerializer.Serialize(enabled));
        Enabled = enabled;
    }

    public string[] Load()
    {
        if (!Enabled || !File.Exists(SidecarPath) || !File.Exists(nativePath)) return Array.Empty<string>();
        var snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(SidecarPath));
        if (snapshot == null || snapshot.Version != 1 || snapshot.SaveHash != Hash() || snapshot.Ids == null)
            return Array.Empty<string>();
        return snapshot.Ids.Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public void Save(IEnumerable<string> ids)
    {
        if (!File.Exists(nativePath)) return;
        AtomicWrite(SidecarPath, JsonSerializer.Serialize(new Snapshot(1, Hash(),
            ids.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray())));
    }

    public void Clear()
    {
        if (File.Exists(SidecarPath)) File.Delete(SidecarPath);
    }

    private string Hash() => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(nativePath)));

    private static void AtomicWrite(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, content);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
