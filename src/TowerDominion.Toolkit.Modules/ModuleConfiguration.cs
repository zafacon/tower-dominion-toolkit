namespace TowerDominion.Toolkit.Modules;

/// <summary>Immutable startup configuration. Unlisted modules are enabled by default.</summary>
public sealed class ModuleConfiguration : IModuleConfiguration
{
    private readonly HashSet<string> disabledModules;

    public ModuleConfiguration(IEnumerable<string>? disabledModuleIds = null)
    {
        disabledModules = new HashSet<string>(disabledModuleIds ?? Array.Empty<string>(), StringComparer.Ordinal);
    }

    public bool IsEnabled(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", nameof(moduleId));
        }
        return !disabledModules.Contains(moduleId);
    }
}
