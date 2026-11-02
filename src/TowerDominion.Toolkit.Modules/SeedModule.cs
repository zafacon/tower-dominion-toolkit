using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Exposes seed override policy through the shared model.</summary>
public sealed class SeedModule : ICapabilityModule
{
    private readonly ISeedService seeds;

    public SeedModule(ISeedService seeds)
    {
        this.seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
    }

    public string Id => "seeds";

    public IReadOnlyCollection<string> RequiredCapabilities => Array.Empty<string>();

    public void Initialize(IGameModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
    }

    public ModelOperation RequestOverride(string seed)
    {
        return seeds.RequestOverride(seed);
    }
}
