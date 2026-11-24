using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngIntegrationBoundaryTests
{
    [Fact]
    public void StableCatalogHasUniqueKeys()
    {
        Assert.Equal(RngCatalog.StableRunKeys.Count, RngCatalog.StableRunKeys.Distinct().Count());
        Assert.All(RngCatalog.StableRunKeys, key => Assert.Equal("Run", key.Scope));
    }

    [Fact]
    public void AttachingLegacySaveDoesNotMaterializeOrAdvanceStreams()
    {
        var factory = new Xoshiro256StarStarFactory();
        var deriver = new Fnv1aKeyDeriver();
        const ulong seed = 123456;

        CorrectiveRngRunState legacy = CorrectiveRngRun.AttachToExistingRun(seed, factory, deriver);
        string pristineJson = CorrectiveRngRun.Save(legacy);

        Assert.Equal(CorrectiveRngLoadMode.ExistingRunWithoutSnapshot, legacy.LoadMode);
        Assert.Empty(RngSnapshotJson.Deserialize(pristineJson).Streams);

        var control = CorrectiveRngRun.Start(seed, factory, deriver);
        Assert.Equal(
            control.Manager.NextUInt64(RngCatalog.Neutral.Identity),
            legacy.Manager.NextUInt64(RngCatalog.Neutral.Identity));
    }

    [Fact]
    public void SaveAndRestoreRunContinuesExactly()
    {
        var factory = new Xoshiro256StarStarFactory();
        var deriver = new Fnv1aKeyDeriver();
        const ulong seed = 987654321;

        CorrectiveRngRunState run = CorrectiveRngRun.Start(seed, factory, deriver);
        RngStream identity = run.Manager.Resolve(RngCatalog.Neutral.Identity);
        RngStream position = run.Manager.Resolve(RngCatalog.Neutral.Position);
        for (int i = 0; i < 17; i++) identity.NextUInt64();
        for (int i = 0; i < 31; i++) position.NextUInt64();

        string json = CorrectiveRngRun.Save(run);
        ulong expectedIdentity = identity.NextUInt64();
        ulong expectedPosition = position.NextUInt64();

        CorrectiveRngRunState restored = CorrectiveRngRun.Start(seed, factory, deriver, json);

        Assert.Equal(CorrectiveRngLoadMode.RestoredSnapshot, restored.LoadMode);
        Assert.Equal(expectedIdentity, restored.Manager.NextUInt64(RngCatalog.Neutral.Identity));
        Assert.Equal(expectedPosition, restored.Manager.NextUInt64(RngCatalog.Neutral.Position));
    }

    [Fact]
    public void UnknownFutureCatalogStreamsNeedNoSnapshotEntry()
    {
        var factory = new Xoshiro256StarStarFactory();
        var deriver = new Fnv1aKeyDeriver();
        const ulong seed = 4444;

        CorrectiveRngRunState run = CorrectiveRngRun.Start(seed, factory, deriver);
        run.Manager.NextUInt64(RngCatalog.Map.ZoneSelection);
        string json = CorrectiveRngRun.Save(run);

        CorrectiveRngRunState restored = CorrectiveRngRun.Start(seed, factory, deriver, json);
        var future = new RngKey("Run", "Neutral", "FutureAppearance");

        var pristine = CorrectiveRngRun.Start(seed, factory, deriver);
        Assert.Equal(pristine.Manager.NextUInt64(future), restored.Manager.NextUInt64(future));
    }
}
