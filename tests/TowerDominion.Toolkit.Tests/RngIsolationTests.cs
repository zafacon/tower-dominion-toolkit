using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngIsolationTests
{
    private static readonly RngKey Identity = new("Run", "Neutral", "Identity");
    private static readonly RngKey Position = new("Run", "Neutral", "Position");

    public static TheoryData<IRandomGeneratorFactory> Factories => new()
    {
        new Pcg32Factory(),
        new Xoshiro256StarStarFactory()
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void SameSeedAndKeyProduceSameSequence(IRandomGeneratorFactory factory)
    {
        var a = Create(12345, factory);
        var b = Create(12345, factory);
        for (int i = 0; i < 10_000; i++)
            Assert.Equal(a.NextUInt64(Identity), b.NextUInt64(Identity));
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void ConsumingOneStreamDoesNotAdvanceAnother(IRandomGeneratorFactory factory)
    {
        var control = Create(12345, factory);
        var changed = Create(12345, factory);

        RandomGeneratorState before = changed.CaptureState(Identity);
        for (int i = 0; i < 50_000; i++)
            changed.NextUInt64(Position);
        RandomGeneratorState after = changed.CaptureState(Identity);

        Assert.Equal(before, after);
        for (int i = 0; i < 1_000; i++)
            Assert.Equal(control.NextUInt64(Identity), changed.NextUInt64(Identity));
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void StreamCreationOrderDoesNotMatter(IRandomGeneratorFactory factory)
    {
        var a = Create(999, factory);
        var b = Create(999, factory);

        ulong aIdentity = a.NextUInt64(Identity);
        ulong aPosition = a.NextUInt64(Position);

        ulong bPosition = b.NextUInt64(Position);
        ulong bIdentity = b.NextUInt64(Identity);

        Assert.Equal(aIdentity, bIdentity);
        Assert.Equal(aPosition, bPosition);
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void SnapshotRestoreContinuesExactly(IRandomGeneratorFactory factory)
    {
        var rng = Create(42, factory);
        for (int i = 0; i < 100; i++)
            rng.NextUInt64(Identity);

        RandomGeneratorState snapshot = rng.CaptureState(Identity);
        ulong[] expected = Enumerable.Range(0, 1000).Select(_ => rng.NextUInt64(Identity)).ToArray();

        rng.RestoreState(Identity, snapshot);
        ulong[] actual = Enumerable.Range(0, 1000).Select(_ => rng.NextUInt64(Identity)).ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void VariablePositionRetriesDoNotChangeIdentity(IRandomGeneratorFactory factory)
    {
        var noRetries = Create(8675309, factory);
        var manyRetries = Create(8675309, factory);

        for (int i = 0; i < 37; i++)
            manyRetries.NextInt32(Position, -2, 3);

        Assert.Equal(noRetries.NextUInt64(Identity), manyRetries.NextUInt64(Identity));
    }


    [Theory]
    [MemberData(nameof(Factories))]
    public void ResolvedHandleMatchesKeyedAccess(IRandomGeneratorFactory factory)
    {
        var keyed = Create(1234, factory);
        var handled = Create(1234, factory);
        RngStream stream = handled.Resolve(Identity);

        for (int i = 0; i < 10_000; i++)
            Assert.Equal(keyed.NextUInt64(Identity), stream.NextUInt64());
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void SnapshotRestoresAllMaterializedStreams(IRandomGeneratorFactory factory)
    {
        var rng = Create(5150, factory);
        RngStream identity = rng.Resolve(Identity);
        RngStream position = rng.Resolve(Position);

        for (int i = 0; i < 73; i++) identity.NextUInt64();
        for (int i = 0; i < 19; i++) position.NextUInt64();

        RngSnapshot snapshot = rng.CaptureSnapshot();
        ulong expectedIdentity = identity.NextUInt64();
        ulong expectedPosition = position.NextUInt64();

        rng.RestoreSnapshot(snapshot);

        Assert.Equal(expectedIdentity, rng.Resolve(Identity).NextUInt64());
        Assert.Equal(expectedPosition, rng.Resolve(Position).NextUInt64());
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void ShuffleIsDeterministicAndConfinedToItsStream(IRandomGeneratorFactory factory)
    {
        var a = Create(2468, factory);
        var b = Create(2468, factory);
        var valuesA = Enumerable.Range(0, 100).ToList();
        var valuesB = Enumerable.Range(0, 100).ToList();

        a.Resolve(Position).Shuffle(valuesA);
        b.Resolve(Position).Shuffle(valuesB);

        Assert.Equal(valuesA, valuesB);
        Assert.Equal(a.NextUInt64(Identity), b.NextUInt64(Identity));
        Assert.Equal(Enumerable.Range(0, 100), valuesA.OrderBy(value => value));
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void IntegerRangeStaysWithinBoundsIncludingFullSignedRange(IRandomGeneratorFactory factory)
    {
        RngStream stream = Create(777, factory).Resolve(Position);

        for (int i = 0; i < 100_000; i++)
        {
            int small = stream.NextInt32(-2, 3);
            Assert.InRange(small, -2, 2);

            int wide = stream.NextInt32(int.MinValue, int.MaxValue);
            Assert.True(wide >= int.MinValue && wide < int.MaxValue);
        }
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void DoubleRangeIsHalfOpenUnitInterval(IRandomGeneratorFactory factory)
    {
        RngStream stream = Create(888, factory).Resolve(Position);
        for (int i = 0; i < 100_000; i++)
        {
            double value = stream.NextDouble();
            Assert.True(value >= 0.0 && value < 1.0);
        }
    }

    [Fact]
    public void KeyDerivationIsSensitiveToEverySemanticComponent()
    {
        var d = new Fnv1aKeyDeriver();
        ulong seed = 123;
        ulong baseline = d.Derive(seed, new("Run", "Neutral", "Identity"));

        Assert.NotEqual(baseline, d.Derive(seed, new("NonRun", "Neutral", "Identity")));
        Assert.NotEqual(baseline, d.Derive(seed, new("Run", "Map", "Identity")));
        Assert.NotEqual(baseline, d.Derive(seed, new("Run", "Neutral", "Position")));
        Assert.Equal(baseline, d.Derive(seed, new("Run", "Neutral", "Identity")));
    }

    private static RngManager Create(ulong seed, IRandomGeneratorFactory factory) =>
        new(seed, new Fnv1aKeyDeriver(), factory);
}
