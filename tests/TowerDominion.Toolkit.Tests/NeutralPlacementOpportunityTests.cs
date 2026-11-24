using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class NeutralPlacementOpportunityTests
{
    [Fact]
    public void ExposedIdentityIsStableAcrossPositionRecomputes()
    {
        RngManager manager = Manager();
        var opportunity = new NeutralPlacementOpportunity(manager);
        int identity = opportunity.ExposeIdentity(7);

        _ = opportunity.RecomputePosition((x, y) => x == 1 && y == 1);
        _ = opportunity.RecomputePosition((x, y) => x == -1 && y == 0);
        _ = opportunity.RecomputePosition((x, y) => x == 0 && y == -2);

        Assert.Equal(identity, opportunity.ExposeIdentity(7));
    }

    [Fact]
    public void PositionRetryCountDoesNotAdvanceIdentity()
    {
        RngManager a = Manager();
        RngManager b = Manager();
        var manyRetries = new NeutralPlacementOpportunity(a);
        var immediate = new NeutralPlacementOpportunity(b);

        _ = manyRetries.RecomputePosition((x, y) => x == 1 && y == 1);
        _ = immediate.RecomputePosition((_, _) => true);

        Assert.Equal(immediate.ExposeIdentity(9), manyRetries.ExposeIdentity(9));
    }

    [Fact]
    public void CommitUsesExactlyExposedIdentityAndLatestPosition()
    {
        var opportunity = new NeutralPlacementOpportunity(Manager());
        int identity = opportunity.ExposeIdentity(6);
        _ = opportunity.RecomputePosition((_, _) => true);
        NeutralPositionDecision latest = opportunity.RecomputePosition((_, _) => true);

        NeutralPlacementDecision committed = opportunity.Commit(6);

        Assert.Equal(identity, committed.IdentityIndex);
        Assert.Equal(latest, committed.Position);
        Assert.True(opportunity.IsCommitted);
    }

    [Fact]
    public void CancellationDoesNotRewindConsumedSemanticStreams()
    {
        RngManager manager = Manager();
        var cancelled = new NeutralPlacementOpportunity(manager);
        int first = cancelled.ExposeIdentity(1000);
        cancelled.Cancel();

        var next = new NeutralPlacementOpportunity(manager);
        int second = next.ExposeIdentity(1000);

        RngManager reference = Manager();
        RngStream identity = reference.Resolve(RngCatalog.Neutral.Identity);
        Assert.Equal(first, identity.NextInt32(0, 1000));
        Assert.Equal(second, identity.NextInt32(0, 1000));
    }

    [Fact]
    public void CommitMaterializesOrientationOnlyAtCommit()
    {
        RngManager manager = Manager();
        var opportunity = new NeutralPlacementOpportunity(manager);
        _ = opportunity.ExposeIdentity(4);
        _ = opportunity.RecomputePosition((_, _) => true);

        Assert.DoesNotContain(
            manager.CaptureSnapshot().Streams,
            stream => stream.Key == RngCatalog.Neutral.Orientation);

        _ = opportunity.Commit(4);

        Assert.Contains(
            manager.CaptureSnapshot().Streams,
            stream => stream.Key == RngCatalog.Neutral.Orientation);
    }

    [Fact]
    public void IncompatibleEligibleSetAfterExposureFailsInsteadOfSilentlyChangingIdentity()
    {
        var opportunity = new NeutralPlacementOpportunity(Manager());
        int identity = opportunity.ExposeIdentity(100);
        int smallerCount = Math.Max(1, identity);

        if (identity == 0)
            return;

        Assert.Throws<InvalidOperationException>(() => opportunity.ExposeIdentity(smallerCount));
    }

    private static RngManager Manager() =>
        new(42, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
}
