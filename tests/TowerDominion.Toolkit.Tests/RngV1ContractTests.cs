using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngV1ContractTests
{
    [Fact]
    public void Fnv1aDerivationHasStableReferenceVectors()
    {
        var deriver = new Fnv1aKeyDeriver();

        Assert.Equal(0x0D1D0153F0E065B3UL,
            deriver.Derive(0, new RngKey("Run", "Neutral", "Identity")));
        Assert.Equal(0x4E985DB330F9E46EUL,
            deriver.Derive(0xFFFFFFFFUL, new RngKey("Run", "Neutral", "Identity")));
        Assert.Equal(0xCC39FA847BEAEBF4UL,
            deriver.Derive(42, new RngKey("Run", "Map", "ZoneSelection")));
    }

    [Fact]
    public void V1IdentifiersAreFrozen()
    {
        Assert.Equal("xoshiro256ss-v1", new Xoshiro256StarStarFactory().AlgorithmId);
        Assert.Equal("fnv1a64+splitmix64-v1", new Fnv1aKeyDeriver().DerivationId);
        Assert.Equal(1, RngManager.CurrentSnapshotFormatVersion);
        Assert.Equal(1, CorrectiveRngSidecarCodec.CurrentSchemaVersion);
        Assert.Equal(1, CorrectiveRngProvenanceCodec.CurrentSchemaVersion);
    }
}
