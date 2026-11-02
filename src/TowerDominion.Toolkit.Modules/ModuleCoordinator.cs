using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>
/// Initializes enabled modules when their requirements become available.
/// Activation settings are fixed for the lifetime of this coordinator.
/// </summary>
public sealed class ModuleCoordinator : IDisposable
{
    private readonly ModuleRegistry registry;
    private readonly IModuleConfiguration configuration;
    private readonly IGameModel model;
    private readonly Dictionary<string, ModuleStartupResult> results = new(StringComparer.Ordinal);
    private bool started;
    private bool disposed;

    public ModuleCoordinator(ModuleRegistry registry, IModuleConfiguration configuration, IGameModel model)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public IReadOnlyCollection<ModuleStartupResult> Results => results.Values.ToArray();

    public event EventHandler<ModuleStartupResult>? ModuleStatusChanged;

    public void Start()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ModuleCoordinator));
        }

        if (started)
        {
            throw new InvalidOperationException("Module coordinator has already started.");
        }

        started = true;
        model.Capabilities.CapabilityChanged += OnCapabilityChanged;
        Evaluate();
    }

    private void OnCapabilityChanged(object? sender, CapabilityChangedEventArgs args)
    {
        Evaluate();
    }

    private void Evaluate()
    {
        foreach (ICapabilityModule module in registry.GetAll())
        {
            if (results.TryGetValue(module.Id, out ModuleStartupResult? previous)
                && previous.Status != ModuleStatus.Unavailable)
            {
                continue;
            }

            if (!configuration.IsEnabled(module.Id))
            {
                results[module.Id] = new(module.Id, ModuleStatus.Disabled);
                continue;
            }

            string? missing = module.RequiredCapabilities.FirstOrDefault(
                id => !model.Capabilities.TryGet(id, out CapabilityDescriptor? descriptor)
                    || descriptor?.Availability != CapabilityAvailability.Available);

            if (missing is not null)
            {
                results[module.Id] = new(module.Id, ModuleStatus.Unavailable, $"Required capability '{missing}' is unavailable.");
                continue;
            }

            try
            {
                module.Initialize(model);
                results[module.Id] = new(module.Id, ModuleStatus.Active);
                ModuleStatusChanged?.Invoke(this, results[module.Id]);
            }
            catch (Exception exception)
            {
                results[module.Id] = new(module.Id, ModuleStatus.Failed, exception.Message);
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (started)
        {
            model.Capabilities.CapabilityChanged -= OnCapabilityChanged;
        }
    }
}
