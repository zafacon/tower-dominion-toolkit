using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Runtime;

/// <summary>Called by a game-specific hook after the game generates its run seed.</summary>
public sealed class SeedRuntimeBridge
{
    private readonly ISeedService seeds;

    public SeedRuntimeBridge(ISeedService seeds)
    {
        this.seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
    }

    public bool AwaitingVerification => seeds.AwaitingVerification;

    public void ObserveEffectiveSeed(string encodedSeed)
    {
        seeds.ObserveEffectiveSeed(encodedSeed);
    }

    public string OnSeedGenerated(string generatedSeed, Func<string, string> setAndReadEncodedSeed)
    {
        string effectiveSeed = generatedSeed;
        seeds.ApplyGeneratedSeed(generatedSeed, requested =>
        {
            effectiveSeed = setAndReadEncodedSeed(requested);
            return effectiveSeed;
        });
        return effectiveSeed;
    }
}
