namespace TowerDominion.Toolkit.Rng;

public sealed record RngSnapshot(
    int FormatVersion,
    string AlgorithmId,
    string DerivationId,
    ulong RootSeed,
    IReadOnlyList<RngStreamSnapshot> Streams);

public sealed record RngStreamSnapshot(RngKey Key, RandomGeneratorState State);
