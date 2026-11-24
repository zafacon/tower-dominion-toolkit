using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngPersistenceAndQualityTests
{
    private static readonly RngKey Key = new("Run", "Neutral", "Identity");

    public static TheoryData<IRandomGeneratorFactory> Factories => new()
    {
        new Pcg32Factory(),
        new Xoshiro256StarStarFactory()
    };

    [Theory]
    [MemberData(nameof(Factories))]
    public void JsonSnapshotRoundTripContinuesExactly(IRandomGeneratorFactory factory)
    {
        var source = Create(0x123456789ABCDEF0UL, factory);
        var identity = source.Resolve(Key);
        var position = source.Resolve(new("Run", "Neutral", "Position"));

        for (int i = 0; i < 137; i++) identity.NextUInt64();
        for (int i = 0; i < 41; i++) position.NextUInt64();

        string json = RngSnapshotJson.Serialize(source.CaptureSnapshot());
        RngSnapshot restoredSnapshot = RngSnapshotJson.Deserialize(json);
        var restored = Create(0x123456789ABCDEF0UL, factory);
        restored.RestoreSnapshot(restoredSnapshot);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(identity.NextUInt64(), restored.Resolve(Key).NextUInt64());
            Assert.Equal(position.NextUInt64(), restored.Resolve(new("Run", "Neutral", "Position")).NextUInt64());
        }
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void SmallBoundedRangeHasReasonableFrequencySanity(IRandomGeneratorFactory factory)
    {
        const int buckets = 7;
        const int samples = 700_000;
        int[] counts = new int[buckets];
        RngStream stream = Create(0xCAFEBABEUL, factory).Resolve(Key);

        for (int i = 0; i < samples; i++)
            counts[stream.NextInt32(0, buckets)]++;

        double expected = samples / (double)buckets;
        double tolerance = expected * 0.015;
        foreach (int count in counts)
            Assert.InRange(count, expected - tolerance, expected + tolerance);
    }

    [Theory]
    [MemberData(nameof(Factories))]
    public void DifferentSemanticKeysDoNotCollapseAcrossSample(IRandomGeneratorFactory factory)
    {
        var rng = Create(0xDEADBEEFUL, factory);
        var seenFirstValues = new HashSet<ulong>();

        for (int i = 0; i < 10_000; i++)
        {
            RngStream stream = rng.Resolve(new("Run", "Synthetic", "Stream-" + i));
            Assert.True(seenFirstValues.Add(stream.NextUInt64()));
        }
    }

    [Fact]
    public void Xoshiro256StarStarMatchesPublishedReferenceZeroSeedExpansionVector()
    {
        // SplitMix64 expansion of seed 0 followed by the xoshiro256** 1.0 transition.
        var rng = new Xoshiro256StarStarGenerator(0);
        ulong[] expected =
        {
            0x99EC5F36CB75F2B4UL,
            0xBF6E1F784956452AUL,
            0x1A5F849D4933E6E0UL,
            0x6AA594F1262D2D2CUL,
            0xBBA5AD4A1F842E59UL
        };

        Assert.Equal(expected, expected.Select(_ => rng.NextUInt64()).ToArray());
    }

    private static RngManager Create(ulong seed, IRandomGeneratorFactory factory) =>
        new(seed, new Fnv1aKeyDeriver(), factory);
}
