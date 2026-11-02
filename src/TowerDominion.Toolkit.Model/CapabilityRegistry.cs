namespace TowerDominion.Toolkit.Model;

/// <summary>Stores the currently known capabilities without implying that they are usable.</summary>
public sealed class CapabilityRegistry : ICapabilityRegistry
{
    private readonly Dictionary<string, CapabilityDescriptor> descriptors = new(StringComparer.Ordinal);

    public event EventHandler<CapabilityChangedEventArgs>? CapabilityChanged;

    public bool TryGet(string capabilityId, out CapabilityDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", nameof(capabilityId));
        }
        return descriptors.TryGetValue(capabilityId, out descriptor);
    }

    public void Set(CapabilityDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (string.IsNullOrWhiteSpace(descriptor.Id))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", nameof(descriptor.Id));
        }
        if (descriptors.TryGetValue(descriptor.Id, out CapabilityDescriptor? existing) && existing == descriptor)
        {
            return;
        }

        descriptors[descriptor.Id] = descriptor;
        CapabilityChanged?.Invoke(this, new CapabilityChangedEventArgs(descriptor));
    }

    public IReadOnlyCollection<CapabilityDescriptor> GetAll()
    {
        return descriptors.Values.ToArray();
    }
}
