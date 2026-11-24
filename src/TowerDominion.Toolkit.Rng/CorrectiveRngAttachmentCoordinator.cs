namespace TowerDominion.Toolkit.Rng;

public enum CorrectiveRngAttachmentStatus
{
    LegacyAttached,
    CorrectiveRestored,
    BlockedMissingSnapshot,
    BlockedInvalidState
}

public sealed record CorrectiveRngAttachmentResult(
    CorrectiveRngAttachmentStatus Status,
    CorrectiveRngAttachmentDecision Decision,
    CorrectiveRngRunState? Run,
    string? Detail)
{
    public bool IsAttached => Run is not null;
}

public sealed class CorrectiveRngAttachmentCoordinator
{
    private readonly CorrectiveRngLifecycle _lifecycle;
    private readonly ICorrectiveRngPersistenceStore _store;

    public CorrectiveRngAttachmentCoordinator(
        CorrectiveRngLifecycle lifecycle,
        ICorrectiveRngPersistenceStore store)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public CorrectiveRngAttachmentResult Attach(CorrectiveRngPersistencePaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (_lifecycle.Phase != CorrectiveRngLifecyclePhase.RootObserved ||
            string.IsNullOrWhiteSpace(_lifecycle.EncodedGameId) ||
            _lifecycle.NativeSeed is not int nativeSeed)
        {
            throw new InvalidOperationException("A native run root must be observed before persistence attachment.");
        }

        if (!_store.Exists(paths.NativeSavePath))
            return Blocked(CorrectiveRngAttachmentDecision.CorrectiveSnapshotInvalid, "Native Progress.gd is missing.");

        byte[] nativeSaveBytes = _store.ReadBytes(paths.NativeSavePath);

        CorrectiveRngProvenance? provenance = null;
        CorrectiveRngProvenanceValidation provenanceValidation = CorrectiveRngProvenanceValidation.Valid;
        try
        {
            if (_store.Exists(paths.ProvenancePath))
            {
                provenance = CorrectiveRngProvenanceCodec.Deserialize(_store.ReadText(paths.ProvenancePath));
                provenanceValidation = CorrectiveRngProvenanceCodec.Validate(
                    provenance, _lifecycle.EncodedGameId, nativeSeed);
            }
        }
        catch (Exception exception)
        {
            return Blocked(CorrectiveRngAttachmentDecision.CorrectiveSnapshotInvalid,
                "Corrective provenance could not be read: " + exception.Message);
        }

        CorrectiveRngSidecar? sidecar = null;
        CorrectiveRngSidecarValidation sidecarValidation = CorrectiveRngSidecarValidation.Valid;
        try
        {
            if (_store.Exists(paths.SidecarPath))
            {
                sidecar = CorrectiveRngSidecarCodec.Deserialize(_store.ReadText(paths.SidecarPath));
                sidecarValidation = CorrectiveRngSidecarCodec.Validate(
                    sidecar, _lifecycle.EncodedGameId, nativeSeed, nativeSaveBytes);
            }
        }
        catch (Exception exception)
        {
            return Blocked(CorrectiveRngAttachmentDecision.CorrectiveSnapshotInvalid,
                "Corrective sidecar could not be read: " + exception.Message);
        }

        CorrectiveRngAttachmentDecision decision = CorrectiveRngProvenanceCodec.DecideAttachment(
            provenance, provenanceValidation, sidecar, sidecarValidation);

        return decision switch
        {
            CorrectiveRngAttachmentDecision.LegacyWithoutCorrectiveHistory =>
                new(CorrectiveRngAttachmentStatus.LegacyAttached, decision, _lifecycle.AttachLegacy(), null),
            CorrectiveRngAttachmentDecision.RestoreCorrectiveSnapshot =>
                new(CorrectiveRngAttachmentStatus.CorrectiveRestored, decision,
                    _lifecycle.Restore(sidecar!.Snapshot), null),
            CorrectiveRngAttachmentDecision.CorrectiveSnapshotRequiredButMissing =>
                Blocked(decision, "This run previously used corrective RNG, but its snapshot is missing."),
            _ => Blocked(decision, DescribeInvalid(provenanceValidation, sidecarValidation))
        };
    }

    private static CorrectiveRngAttachmentResult Blocked(
        CorrectiveRngAttachmentDecision decision,
        string detail) =>
        new(
            decision == CorrectiveRngAttachmentDecision.CorrectiveSnapshotRequiredButMissing
                ? CorrectiveRngAttachmentStatus.BlockedMissingSnapshot
                : CorrectiveRngAttachmentStatus.BlockedInvalidState,
            decision,
            null,
            detail);

    private static string DescribeInvalid(
        CorrectiveRngProvenanceValidation provenance,
        CorrectiveRngSidecarValidation sidecar) =>
        $"Corrective persistence validation failed (provenance={provenance}, sidecar={sidecar}).";
}
