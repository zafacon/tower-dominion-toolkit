namespace TowerDominion.Toolkit.Model;

public enum CapabilityAvailability
{
    Available,
    TemporarilyUnavailable,
    Unsupported
}

public sealed record CapabilityDescriptor(
    string Id,
    CapabilityAvailability Availability,
    string? Reason = null);

public interface ICapabilityRegistry
{
    event EventHandler<CapabilityChangedEventArgs>? CapabilityChanged;

    bool TryGet(string capabilityId, out CapabilityDescriptor? descriptor);
}

public interface IGameModel
{
    ICapabilityRegistry Capabilities { get; }
}

public sealed class CapabilityChangedEventArgs : EventArgs
{
    public CapabilityChangedEventArgs(CapabilityDescriptor capability)
    {
        Capability = capability;
    }

    public CapabilityDescriptor Capability { get; }
}
