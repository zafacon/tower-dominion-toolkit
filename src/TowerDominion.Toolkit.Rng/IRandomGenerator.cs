namespace TowerDominion.Toolkit.Rng;

public interface IRandomGenerator
{
    ulong NextUInt64();
    RandomGeneratorState CaptureState();
    void RestoreState(RandomGeneratorState state);
}

public readonly record struct RandomGeneratorState(ulong State0, ulong State1, ulong State2, ulong State3);
