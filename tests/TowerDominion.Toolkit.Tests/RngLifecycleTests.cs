using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngLifecycleTests
{
    [Fact]
    public void ObserveRootDoesNotAttachOrConsumeRng()
    {
        CorrectiveRngLifecycle lifecycle = Create();

        lifecycle.ObserveRoot("GAME", -7);

        Assert.Equal(CorrectiveRngLifecyclePhase.RootObserved, lifecycle.Phase);
        Assert.Null(lifecycle.Current);
        Assert.Equal(-7, lifecycle.NativeSeed);
    }

    [Fact]
    public void NewRootInvalidatesPreviousRunAndHandles()
    {
        CorrectiveRngLifecycle lifecycle = Create();
        lifecycle.ObserveRoot("A", 1);
        CorrectiveRngRunState first = lifecycle.StartCorrective();
        RngStream oldHandle = first.Manager.Resolve(RngCatalog.Neutral.Identity);
        _ = oldHandle.NextUInt64();

        lifecycle.ObserveRoot("B", 2);

        Assert.Null(lifecycle.Current);
        Assert.Equal(CorrectiveRngLifecyclePhase.RootObserved, lifecycle.Phase);
        CorrectiveRngRunState second = lifecycle.StartCorrective();
        Assert.NotSame(first.Manager, second.Manager);
    }

    [Fact]
    public void CannotAttachBeforeRootObservation()
    {
        CorrectiveRngLifecycle lifecycle = Create();

        Assert.Throws<InvalidOperationException>(() => lifecycle.AttachLegacy());
    }

    [Fact]
    public void ClearRemovesObservedIdentityAndRun()
    {
        CorrectiveRngLifecycle lifecycle = Create();
        lifecycle.ObserveRoot("A", 1);
        lifecycle.StartCorrective();

        lifecycle.Clear();

        Assert.Equal(CorrectiveRngLifecyclePhase.Detached, lifecycle.Phase);
        Assert.Null(lifecycle.EncodedGameId);
        Assert.Null(lifecycle.NativeSeed);
        Assert.Null(lifecycle.Current);
    }

    private static CorrectiveRngLifecycle Create() =>
        new(new CorrectiveRngSession(new Xoshiro256StarStarFactory(), new Fnv1aKeyDeriver()));
}

public sealed class RngActivationLifecycleTests
{
    [Fact]
    public void ActivatingLoadedLegacyRunPreservesExistingManagerAndState()
    {
        var lifecycle = new CorrectiveRngLifecycle(
            new CorrectiveRngSession(new Xoshiro256StarStarFactory(), new Fnv1aKeyDeriver()));
        lifecycle.ObserveRoot("game", 42);
        CorrectiveRngRunState legacy = lifecycle.AttachLegacy();
        RngStream stream = legacy.Manager.Resolve(RngCatalog.Neutral.Identity);
        _ = stream.NextUInt64();
        RngSnapshot checkpoint = legacy.Manager.CaptureSnapshot();
        ulong expectedNext = stream.NextUInt64();
        legacy.Manager.RestoreSnapshot(checkpoint);

        CorrectiveRngRunState activated = lifecycle.ActivateCorrective();

        Assert.Same(legacy, activated);
        Assert.Same(legacy.Manager, activated.Manager);
        Assert.Equal(expectedNext, activated.Manager.Resolve(RngCatalog.Neutral.Identity).NextUInt64());
    }

    [Fact]
    public void ActivatingFreshObservedRunCreatesPristineManager()
    {
        var lifecycle = new CorrectiveRngLifecycle(
            new CorrectiveRngSession(new Xoshiro256StarStarFactory(), new Fnv1aKeyDeriver()));
        lifecycle.ObserveRoot("game", 42);

        CorrectiveRngRunState activated = lifecycle.ActivateCorrective();

        Assert.Equal(CorrectiveRngLoadMode.NewRun, activated.LoadMode);
        Assert.Empty(activated.Manager.CaptureSnapshot().Streams);
    }
}
