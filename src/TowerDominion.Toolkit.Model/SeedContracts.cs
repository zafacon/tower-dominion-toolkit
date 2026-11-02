namespace TowerDominion.Toolkit.Model;

/// <summary>Game-facing seed operations, with no dependency on Unity or mod loaders.</summary>
public interface ISeedService
{
    ModelProperty<string> Seed { get; }

    ModelOperation RequestOverride(string seed);

    bool ClearOverride();

    string? PendingOverride { get; }

    bool AwaitingVerification { get; }

    void ObserveEffectiveSeed(string encodedSeed);

    void ApplyGeneratedSeed(string generatedSeed, Func<string, string> applyOverride);
}
