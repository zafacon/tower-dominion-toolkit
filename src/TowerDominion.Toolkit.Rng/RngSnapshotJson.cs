using System.Text.Json;

namespace TowerDominion.Toolkit.Rng;

public static class RngSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(RngSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static RngSnapshot Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Snapshot JSON cannot be null or whitespace.", nameof(json));
        return JsonSerializer.Deserialize<RngSnapshot>(json, Options)
            ?? throw new InvalidOperationException("RNG snapshot JSON produced no value.");
    }
}
