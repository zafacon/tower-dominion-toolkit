namespace TowerDominion.Toolkit.Rng;

/// <summary>Resolved handle to one independently advancing semantic random stream.</summary>
public sealed class RngStream
{
    private readonly IRandomGenerator _generator;

    internal RngStream(RngKey key, IRandomGenerator generator)
    {
        Key = key;
        _generator = generator;
    }

    public RngKey Key { get; }

    public ulong NextUInt64() => _generator.NextUInt64();

    public int NextInt32(int minInclusive, int maxExclusive)
    {
        long width = (long)maxExclusive - minInclusive;
        if (width <= 0 || width > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));

        ulong range = (ulong)width;
        ulong threshold = unchecked(0UL - range) % range;
        ulong value;
        do value = _generator.NextUInt64(); while (value < threshold);
        return (int)(minInclusive + (long)(value % range));
    }

    public double NextDouble() => (_generator.NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public void Shuffle<T>(IList<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = NextInt32(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    public RandomGeneratorState CaptureState() => _generator.CaptureState();

    public void RestoreState(RandomGeneratorState state) => _generator.RestoreState(state);
}
