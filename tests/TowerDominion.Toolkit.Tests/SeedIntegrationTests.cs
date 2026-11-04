using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;
using TowerDominion.Toolkit.Runtime;

namespace TowerDominion.Toolkit.Tests;

public sealed class SeedIntegrationTests
{
    [Fact]
    public void OverrideIsAppliedButNotIndependentlyVerified()
    {
        SeedService seeds = new();
        SeedModule module = new(seeds);
        ModelOperation operation = module.RequestOverride("123");
        Assert.Equal(OperationStatus.Accepted, operation.Result.Status);

        SeedRuntimeBridge runtime = new(seeds);
        string result = runtime.OnSeedGenerated("random", requested => "encoded-" + requested);

        Assert.Equal("encoded-123", result);
        Assert.Equal(OperationStatus.Applied, operation.Result.Status);
        Assert.True(runtime.AwaitingVerification);
        Assert.Equal("encoded-123", seeds.Seed.Snapshot.Observed);
        Assert.Equal(PropertyAuthority.Game, seeds.Seed.Snapshot.Authority);
    }

    [Fact]
    public void SameEncodedSeedStillRequiresIndependentVerification()
    {
        SeedService seeds = new();
        ModelOperation operation = seeds.RequestOverride("123");
        SeedRuntimeBridge bridge = new(seeds);
        string result = bridge.OnSeedGenerated("same", _ => "same");

        Assert.Equal("same", result);
        Assert.True(bridge.AwaitingVerification);
        Assert.Equal(OperationStatus.Applied, operation.Result.Status);

        bridge.ObserveEffectiveSeed("same");
        Assert.Equal(OperationStatus.Verified, operation.Result.Status);
        Assert.True(seeds.Seed.Snapshot.IsVerified);
    }

    [Fact]
    public void LaterMatchingObservationVerifiesOverride()
    {
        SeedService seeds = new();
        ModelOperation operation = seeds.RequestOverride("123");
        SeedRuntimeBridge bridge = new(seeds);
        bridge.OnSeedGenerated("random", _ => "encoded-123");

        bridge.ObserveEffectiveSeed("encoded-123");

        Assert.Equal(OperationStatus.Verified, operation.Result.Status);
        Assert.False(bridge.AwaitingVerification);
        Assert.True(seeds.Seed.Snapshot.IsVerified);
    }

    [Fact]
    public void LaterMismatchedObservationFailsVerification()
    {
        SeedService seeds = new();
        ModelOperation operation = seeds.RequestOverride("123");
        SeedRuntimeBridge bridge = new(seeds);
        bridge.OnSeedGenerated("random", _ => "encoded-123");

        bridge.ObserveEffectiveSeed("different");

        Assert.Equal(OperationStatus.Failed, operation.Result.Status);
        Assert.False(seeds.Seed.Snapshot.IsVerified);
    }

    [Fact]
    public void NoOverridePreservesGeneratedSeed()
    {
        SeedService seeds = new();
        SeedRuntimeBridge runtime = new(seeds);
        string result = runtime.OnSeedGenerated("random", _ => throw new InvalidOperationException());

        Assert.Equal("random", result);
        Assert.Equal("random", seeds.Seed.Snapshot.Observed);
    }

    [Fact]
    public void RuntimeFailureDoesNotClaimApplication()
    {
        SeedService seeds = new();
        ModelOperation operation = seeds.RequestOverride("bad");
        SeedRuntimeBridge runtime = new(seeds);

        string result = runtime.OnSeedGenerated("random", _ => throw new InvalidOperationException("Invalid seed"));

        Assert.Equal("random", result);
        Assert.Equal(OperationStatus.Failed, operation.Result.Status);
        Assert.Equal("Invalid seed", operation.Result.Reason);
    }

    [Fact]
    public void ConfiguredSeedPersistsAcrossRunsUntilExplicitlyCleared()
    {
        SeedService seeds = new();
        SeedRuntimeBridge bridge = new(seeds);
        ModelOperation first = seeds.RequestOverride("42");

        Assert.Equal("encoded-42", bridge.OnSeedGenerated("random-1", value => "encoded-" + value));
        bridge.ObserveEffectiveSeed("encoded-42");
        Assert.Equal(OperationStatus.Verified, first.Result.Status);
        Assert.Equal("42", seeds.PendingOverride);

        Assert.Equal("encoded-42", bridge.OnSeedGenerated("random-2", value => "encoded-" + value));
        bridge.ObserveEffectiveSeed("encoded-42");
        Assert.True(seeds.Seed.Snapshot.IsVerified);
        Assert.Equal("42", seeds.PendingOverride);

        Assert.True(seeds.ClearOverride());
        Assert.Null(seeds.PendingOverride);
        Assert.Equal("random-3", bridge.OnSeedGenerated("random-3", _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void ReplacingPersistentSeedChangesSubsequentRuns()
    {
        SeedService seeds = new();
        SeedRuntimeBridge bridge = new(seeds);
        seeds.RequestOverride("first");
        Assert.Equal("first", bridge.OnSeedGenerated("random", value => value));
        seeds.RequestOverride("second");
        Assert.Equal("second", bridge.OnSeedGenerated("random", value => value));
        Assert.Equal("second", seeds.PendingOverride);
    }

    [Fact]
    public void LatestRequestSupersedesEarlierRequest()
    {
        SeedService seeds = new();
        ModelOperation first = seeds.RequestOverride("1");
        ModelOperation second = seeds.RequestOverride("2");

        Assert.Equal(OperationStatus.Failed, first.Result.Status);
        Assert.Equal(OperationStatus.Accepted, second.Result.Status);
    }
}
