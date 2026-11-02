using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

public enum ModuleStatus
{
    Disabled,
    Unavailable,
    Active,
    Failed
}

public sealed record ModuleStartupResult(string Id, ModuleStatus Status, string? Reason = null);

/// <summary>Starts each registered module independently, using a snapshot of startup configuration.</summary>
public sealed class ModuleStartup
{
    public static IReadOnlyCollection<ModuleStartupResult> Start(
        ModuleRegistry registry,
        IModuleConfiguration configuration,
        IGameModel model)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(model);

        List<ModuleStartupResult> results = new();

        foreach (ICapabilityModule module in registry.GetAll())
        {
            if (!configuration.IsEnabled(module.Id))
            {
                results.Add(new(module.Id, ModuleStatus.Disabled));
                continue;
            }

            string? missingCapability = module.RequiredCapabilities.FirstOrDefault(
                capabilityId => !model.Capabilities.TryGet(capabilityId, out CapabilityDescriptor? descriptor)
                    || descriptor?.Availability != CapabilityAvailability.Available);

            if (missingCapability is not null)
            {
                results.Add(new(module.Id, ModuleStatus.Unavailable, $"Required capability '{missingCapability}' is unavailable."));
                continue;
            }

            try
            {
                module.Initialize(model);
                results.Add(new(module.Id, ModuleStatus.Active));
            }
            catch (Exception exception)
            {
                results.Add(new(module.Id, ModuleStatus.Failed, exception.Message));
            }
        }

        return results.AsReadOnly();
    }
}
