using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngSessionTests
{
    [Fact]
    public void ClearDropsRunOwnership()
    {
        var session = Create();
        session.StartNew(42);

        session.Clear();

        Assert.False(session.IsAttached);
        Assert.Null(session.Current);
    }

    [Fact]
    public void StartingAnotherRunReplacesPreviousManager()
    {
        var session = Create();
        CorrectiveRngRunState first = session.StartNew(42);
        first.Manager.NextUInt64(RngCatalog.Neutral.Identity);

        CorrectiveRngRunState second = session.StartNew(43);

        Assert.NotSame(first.Manager, second.Manager);
        Assert.Equal(NativeRunSeed.ToRootSeed(43), second.RootSeed);
        Assert.Empty(second.Manager.CaptureSnapshot().Streams);
    }

    [Fact]
    public void RestoredSessionContinuesSnapshot()
    {
        var source = Create();
        CorrectiveRngRunState run = source.StartNew(-1);
        for (int i = 0; i < 9; i++)
            run.Manager.NextUInt64(RngCatalog.Map.ZoneSelection);
        RngSnapshot snapshot = run.Manager.CaptureSnapshot();
        ulong expected = run.Manager.NextUInt64(RngCatalog.Map.ZoneSelection);

        var restored = Create();
        CorrectiveRngRunState attached = restored.Restore(-1, snapshot);

        Assert.Equal(expected, attached.Manager.NextUInt64(RngCatalog.Map.ZoneSelection));
    }

    private static CorrectiveRngSession Create() =>
        new(new Xoshiro256StarStarFactory(), new Fnv1aKeyDeriver());
}
