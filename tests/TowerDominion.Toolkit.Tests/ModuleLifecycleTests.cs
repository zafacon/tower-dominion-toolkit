using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ModuleLifecycleTests
{
    [Fact]
    public void DeferredModuleInitializesOnceWhenCapabilityAppears()
    {
        CapabilityRegistry capabilities = new();
        GameModel model = new(capabilities);
        FakeModule module = new();
        ModuleRegistry registry = new();
        registry.Register(module);

        using ModuleCoordinator coordinator = new(registry, new ModuleConfiguration(), model);
        coordinator.Start();
        Assert.Equal(ModuleStatus.Unavailable, Assert.Single(coordinator.Results).Status);

        capabilities.Set(new("run.seed", CapabilityAvailability.Available));
        capabilities.Set(new("run.seed", CapabilityAvailability.TemporarilyUnavailable));
        capabilities.Set(new("run.seed", CapabilityAvailability.Available));

        Assert.Equal(ModuleStatus.Active, Assert.Single(coordinator.Results).Status);
        Assert.Equal(1, module.InitializationCount);
    }

    [Fact]
    public void DeferredActivationNotifiesOnceAndDoesNotNotifyAfterDisposal()
    {
        CapabilityRegistry capabilities = new();
        FakeModule module = new();
        ModuleRegistry registry = new();
        registry.Register(module);
        ModuleCoordinator coordinator = new(registry, new ModuleConfiguration(), new GameModel(capabilities));
        List<ModuleStartupResult> notifications = new();
        coordinator.ModuleStatusChanged += (_, result) => notifications.Add(result);
        coordinator.Start();

        Assert.Empty(notifications);
        capabilities.Set(new("run.seed", CapabilityAvailability.Available));
        capabilities.Set(new("run.seed", CapabilityAvailability.TemporarilyUnavailable));
        capabilities.Set(new("run.seed", CapabilityAvailability.Available));
        Assert.Single(notifications);
        Assert.Equal(ModuleStatus.Active, notifications[0].Status);

        coordinator.Dispose();
        capabilities.Set(new("run.seed", CapabilityAvailability.Available));
        Assert.Single(notifications);
    }

    private static readonly string[] disabledModuleIds = new[] { "seeds" };

    [Fact]
    public void DisabledModuleStaysDisabledWhenCapabilityAppears()
    {
        CapabilityRegistry capabilities = new();
        FakeModule module = new();
        ModuleRegistry registry = new();
        registry.Register(module);

        using ModuleCoordinator coordinator = new(registry, new ModuleConfiguration(disabledModuleIds), new GameModel(capabilities));
        coordinator.Start();
        capabilities.Set(new("run.seed", CapabilityAvailability.Available));

        Assert.Equal(ModuleStatus.Disabled, Assert.Single(coordinator.Results).Status);
        Assert.Equal(0, module.InitializationCount);
    }

    [Fact]
    public void CoordinatorUnsubscribesWhenDisposed()
    {
        CapabilityRegistry capabilities = new();
        FakeModule module = new();
        ModuleRegistry registry = new();
        registry.Register(module);

        ModuleCoordinator coordinator = new(registry, new ModuleConfiguration(), new GameModel(capabilities));
        coordinator.Start();
        coordinator.Dispose();
        capabilities.Set(new("run.seed", CapabilityAvailability.Available));

        Assert.Equal(0, module.InitializationCount);
    }

    [Fact]
    public void ConfigurationPersistsAcrossReload()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-toolkit-" + Guid.NewGuid().ToString("N"));
        try
        {
            ModuleConfigurationStore store = new(Path.Combine(directory, "modules.json"));
            Assert.True(store.Load().IsEnabled("seeds"));
            store.Save(disabledModuleIds);
            Assert.False(store.Load().IsEnabled("seeds"));
            Assert.True(store.Load().IsEnabled("other"));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class FakeModule : ICapabilityModule
    {
        public string Id => "seeds";

        public IReadOnlyCollection<string> RequiredCapabilities => new[] { "run.seed" };

        public int InitializationCount { get; private set; }

        public void Initialize(IGameModel model)
        {
            InitializationCount++;
        }
    }
}
