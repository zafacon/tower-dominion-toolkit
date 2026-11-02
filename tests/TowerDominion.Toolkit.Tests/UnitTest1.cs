using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Tests;

public sealed class CapabilityContractTests
{
    [Fact]
    public void CapabilityDescriptorPreservesAvailability()
    {
        CapabilityDescriptor capability = new("game.run.seed", CapabilityAvailability.TemporarilyUnavailable, "No run setup active");

        Assert.Equal("game.run.seed", capability.Id);
        Assert.Equal(CapabilityAvailability.TemporarilyUnavailable, capability.Availability);
        Assert.Equal("No run setup active", capability.Reason);
    }
}
