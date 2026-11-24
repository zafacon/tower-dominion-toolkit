namespace TowerDominion.Toolkit.Rng;

public sealed class CorrectiveRngPersistenceCoordinator
{
    private readonly CorrectiveRngLifecycle _lifecycle;
    private readonly ICorrectiveRngPersistenceStore _store;

    public CorrectiveRngPersistenceCoordinator(
        CorrectiveRngLifecycle lifecycle,
        ICorrectiveRngPersistenceStore store)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public bool IsCorrectiveRequired(CorrectiveRngPersistencePaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return _store.Exists(paths.ProvenancePath);
    }

    public void Activate(CorrectiveRngPersistencePaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        (string gameId, int nativeSeed, CorrectiveRngRunState run) = RequireAttached();

        // Activation is one-way for this run. Write the durable marker first so a crash
        // can never erase evidence that corrective state is now required.
        CorrectiveRngProvenance provenance =
            CorrectiveRngProvenanceCodec.MarkCorrective(gameId, nativeSeed);
        _store.WriteTextAtomic(paths.ProvenancePath, CorrectiveRngProvenanceCodec.Serialize(provenance));

        // The current native save generation must immediately receive a matching snapshot.
        PersistSnapshot(paths, gameId, nativeSeed, run);
    }

    public void PersistAfterNativeSave(CorrectiveRngPersistencePaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (!_store.Exists(paths.ProvenancePath))
            return;

        (string gameId, int nativeSeed, CorrectiveRngRunState run) = RequireAttached();
        PersistSnapshot(paths, gameId, nativeSeed, run);
    }

    private void PersistSnapshot(
        CorrectiveRngPersistencePaths paths,
        string gameId,
        int nativeSeed,
        CorrectiveRngRunState run)
    {
        if (!_store.Exists(paths.NativeSavePath))
            throw new InvalidOperationException("Cannot persist corrective RNG without native Progress.gd.");

        byte[] nativeSave = _store.ReadBytes(paths.NativeSavePath);
        CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Create(
            gameId,
            nativeSeed,
            nativeSave,
            run.Manager.CaptureSnapshot());
        _store.WriteTextAtomic(paths.SidecarPath, CorrectiveRngSidecarCodec.Serialize(sidecar));
    }

    private (string GameId, int NativeSeed, CorrectiveRngRunState Run) RequireAttached()
    {
        if (_lifecycle.Phase != CorrectiveRngLifecyclePhase.Attached ||
            string.IsNullOrWhiteSpace(_lifecycle.EncodedGameId) ||
            _lifecycle.NativeSeed is not int nativeSeed ||
            _lifecycle.Current is not CorrectiveRngRunState run)
        {
            throw new InvalidOperationException("Corrective RNG must be attached before it can be activated or persisted.");
        }

        return (_lifecycle.EncodedGameId, nativeSeed, run);
    }
}
