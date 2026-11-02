using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ModuleStartupTests
{
    [Fact]
    public void UnlistedModuleStartsWhenRequirementsAreAvailable()
    {
        CapabilityRegistry capabilities = new();
        capabilities.Set(new("game.run.seed", CapabilityAvailability.Available));
        GameModel model = new(capabilities);
        FakeModule module = new("seeds", "game.run.seed");
        ModuleRegistry registry = new();
        registry.Register(module);

        ModuleStartupResult result = Assert.Single(ModuleStartup.Start(registry, new ModuleConfiguration(), model));

        Assert.Equal(ModuleStatus.Active, result.Status);
        Assert.Equal(1, module.InitializationCount);
    }

    private static readonly string[] disabledModuleIds = new[] { "seeds" };

    [Fact]
    public void DisabledModuleNeverInitializes()
    {
        FakeModule module = new("seeds", "game.run.seed");
        ModuleRegistry registry = new();
        registry.Register(module);

        ModuleStartupResult result = Assert.Single(ModuleStartup.Start(
            registry, new ModuleConfiguration(disabledModuleIds), new GameModel(new CapabilityRegistry())));

        Assert.Equal(ModuleStatus.Disabled, result.Status);
        Assert.Equal(0, module.InitializationCount);
    }

    [Theory]
    [InlineData(CapabilityAvailability.TemporarilyUnavailable)]
    [InlineData(CapabilityAvailability.Unsupported)]
    public void UnavailableCapabilityPreventsInitialization(CapabilityAvailability availability)
    {
        CapabilityRegistry capabilities = new();
        capabilities.Set(new("game.run.seed", availability));
        FakeModule module = new("seeds", "game.run.seed");
        ModuleRegistry registry = new();
        registry.Register(module);

        ModuleStartupResult result = Assert.Single(ModuleStartup.Start(
            registry, new ModuleConfiguration(), new GameModel(capabilities)));

        Assert.Equal(ModuleStatus.Unavailable, result.Status);
        Assert.Equal(0, module.InitializationCount);
    }

    [Fact]
    public void MissingCapabilityPreventsInitialization()
    {
        FakeModule module = new("seeds", "game.run.seed");
        ModuleRegistry registry = new();
        registry.Register(module);

        ModuleStartupResult result = Assert.Single(ModuleStartup.Start(
            registry, new ModuleConfiguration(), new GameModel(new CapabilityRegistry())));

        Assert.Equal(ModuleStatus.Unavailable, result.Status);
    }

    [Fact]
    public void ModuleFailureDoesNotStopOtherModules()
    {
        ModuleRegistry registry = new();
        registry.Register(new FakeModule("broken", fail: true));
        FakeModule healthy = new("healthy");
        registry.Register(healthy);

        ModuleStartupResult[] results = ModuleStartup.Start(
            registry, new ModuleConfiguration(), new GameModel(new CapabilityRegistry())).ToArray();

        Assert.Equal(ModuleStatus.Failed, results[0].Status);
        Assert.Equal(ModuleStatus.Active, results[1].Status);
        Assert.Equal(1, healthy.InitializationCount);
    }

    [Fact]
    public void DuplicateModuleIdIsRejected()
    {
        ModuleRegistry registry = new();
        registry.Register(new FakeModule("seeds"));

        Assert.Throws<InvalidOperationException>(() => registry.Register(new FakeModule("seeds")));
    }

    [Fact]
    public void CapabilityRegistryUpdatesAvailability()
    {
        CapabilityRegistry registry = new();
        registry.Set(new("game.run.seed", CapabilityAvailability.TemporarilyUnavailable));
        registry.Set(new("game.run.seed", CapabilityAvailability.Available));

        Assert.True(registry.TryGet("game.run.seed", out CapabilityDescriptor? descriptor));
        Assert.Equal(CapabilityAvailability.Available, descriptor?.Availability);
        Assert.Single(registry.GetAll());
    }

    private sealed class FakeModule : ICapabilityModule
    {
        private readonly bool fail;

        public FakeModule(string id, string? requiredCapability = null, bool fail = false)
        {
            Id = id;
            RequiredCapabilities = requiredCapability is null ? Array.Empty<string>() : new[] { requiredCapability };
            this.fail = fail;
        }

        public string Id { get; }

        public IReadOnlyCollection<string> RequiredCapabilities { get; }

        public int InitializationCount { get; private set; }

        public void Initialize(IGameModel model)
        {
            InitializationCount++;

            if (fail)
            {
                throw new InvalidOperationException("Initialization failed.");
            }
        }
    }
}
