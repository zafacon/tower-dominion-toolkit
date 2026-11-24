namespace TowerDominion.Toolkit.Rng;

public sealed class Xoshiro256StarStarGenerator : IRandomGenerator
{
    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    public Xoshiro256StarStarGenerator(ulong seed)
    {
        ulong state = seed;
        _s0 = SplitMix64.Next(ref state);
        _s1 = SplitMix64.Next(ref state);
        _s2 = SplitMix64.Next(ref state);
        _s3 = SplitMix64.Next(ref state);
    }

    public ulong NextUInt64()
    {
        ulong result = RotateLeft(_s1 * 5, 7) * 9;
        ulong t = _s1 << 17;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = RotateLeft(_s3, 45);
        return result;
    }

    public RandomGeneratorState CaptureState() => new(_s0, _s1, _s2, _s3);

    public void RestoreState(RandomGeneratorState state)
    {
        if ((state.State0 | state.State1 | state.State2 | state.State3) == 0)
            throw new ArgumentException("xoshiro state cannot be all zero.", nameof(state));
        _s0 = state.State0; _s1 = state.State1; _s2 = state.State2; _s3 = state.State3;
    }

    private static ulong RotateLeft(ulong value, int count) =>
        (value << count) | (value >> (64 - count));
}

public sealed class Xoshiro256StarStarFactory : IRandomGeneratorFactory
{
    public string AlgorithmId => "xoshiro256ss-v1";
    public IRandomGenerator Create(ulong seed) => new Xoshiro256StarStarGenerator(seed);
}
