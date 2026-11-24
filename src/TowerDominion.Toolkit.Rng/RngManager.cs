namespace TowerDominion.Toolkit.Rng;

public sealed class RngManager
{
    public const int CurrentSnapshotFormatVersion = 1;

    private readonly ulong _rootSeed;
    private readonly IRngKeyDeriver _deriver;
    private readonly IRandomGeneratorFactory _factory;
    private readonly Dictionary<RngKey, RngStream> _streams = new();

    public RngManager(ulong rootSeed, IRngKeyDeriver deriver, IRandomGeneratorFactory factory)
    {
        _rootSeed = rootSeed;
        _deriver = deriver;
        _factory = factory;
    }

    public RngStream Resolve(RngKey key)
    {
        if (_streams.TryGetValue(key, out RngStream? stream))
            return stream;

        stream = new RngStream(key, _factory.Create(_deriver.Derive(_rootSeed, key)));
        _streams.Add(key, stream);
        return stream;
    }

    public ulong NextUInt64(RngKey key) => Resolve(key).NextUInt64();

    public int NextInt32(RngKey key, int minInclusive, int maxExclusive) =>
        Resolve(key).NextInt32(minInclusive, maxExclusive);

    public RandomGeneratorState CaptureState(RngKey key) => Resolve(key).CaptureState();

    public void RestoreState(RngKey key, RandomGeneratorState state) => Resolve(key).RestoreState(state);

    public RngSnapshot CaptureSnapshot() =>
        new(
            CurrentSnapshotFormatVersion,
            _factory.AlgorithmId,
            _deriver.DerivationId,
            _rootSeed,
            _streams
                .OrderBy(pair => pair.Key.Scope, StringComparer.Ordinal)
                .ThenBy(pair => pair.Key.Domain, StringComparer.Ordinal)
                .ThenBy(pair => pair.Key.Stream, StringComparer.Ordinal)
                .Select(pair => new RngStreamSnapshot(pair.Key, pair.Value.CaptureState()))
                .ToArray());

    public void RestoreSnapshot(RngSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.FormatVersion != CurrentSnapshotFormatVersion)
            throw new InvalidOperationException($"Unsupported RNG snapshot format {snapshot.FormatVersion}.");
        if (!string.Equals(snapshot.AlgorithmId, _factory.AlgorithmId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Snapshot algorithm '{snapshot.AlgorithmId}' does not match '{_factory.AlgorithmId}'.");
        if (!string.Equals(snapshot.DerivationId, _deriver.DerivationId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Snapshot derivation '{snapshot.DerivationId}' does not match '{_deriver.DerivationId}'.");
        if (snapshot.RootSeed != _rootSeed)
            throw new InvalidOperationException("Snapshot root seed does not match this RNG manager.");

        _streams.Clear();
        foreach (RngStreamSnapshot saved in snapshot.Streams)
            Resolve(saved.Key).RestoreState(saved.State);
    }
}
