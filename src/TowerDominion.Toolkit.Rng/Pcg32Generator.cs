namespace TowerDominion.Toolkit.Rng;

public sealed class Pcg32Generator : IRandomGenerator
{
    private const ulong Multiplier = 6364136223846793005UL;
    private ulong _state;
    private readonly ulong _increment;

    public Pcg32Generator(ulong seed)
    {
        _increment = 1442695040888963407UL;
        _state = 0;
        NextUInt32();
        _state += seed;
        NextUInt32();
    }

    private Pcg32Generator(ulong state, ulong increment)
    {
        _state = state;
        _increment = increment;
    }

    public ulong NextUInt64() => ((ulong)NextUInt32() << 32) | NextUInt32();

    public RandomGeneratorState CaptureState() => new(_state, _increment, 0, 0);

    public void RestoreState(RandomGeneratorState state)
    {
        if (state.State1 != _increment)
            throw new ArgumentException("State belongs to a different PCG stream.", nameof(state));
        _state = state.State0;
    }

    private uint NextUInt32()
    {
        ulong oldState = _state;
        _state = unchecked(oldState * Multiplier + _increment);
        uint xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rotation = (int)(oldState >> 59);
        return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
    }

    public static IRandomGenerator Restore(RandomGeneratorState state) =>
        new Pcg32Generator(state.State0, state.State1);
}

public sealed class Pcg32Factory : IRandomGeneratorFactory
{
    public string AlgorithmId => "pcg32-v1";
    public IRandomGenerator Create(ulong seed) => new Pcg32Generator(seed);
}
