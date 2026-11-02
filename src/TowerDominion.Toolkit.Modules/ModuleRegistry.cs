namespace TowerDominion.Toolkit.Modules;

/// <summary>Registers modules once; activation is performed separately at startup.</summary>
public sealed class ModuleRegistry
{
    private readonly Dictionary<string, ICapabilityModule> modules = new(StringComparer.Ordinal);

    public void Register(ICapabilityModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (string.IsNullOrWhiteSpace(module.Id))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", nameof(module.Id));
        }

        if (!modules.TryAdd(module.Id, module))
        {
            throw new InvalidOperationException($"Module '{module.Id}' is already registered.");
        }
    }

    public IReadOnlyCollection<ICapabilityModule> GetAll()
    {
        return modules.Values.ToArray();
    }
}
