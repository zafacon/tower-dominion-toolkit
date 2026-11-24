using System.Text.Json;

namespace TowerDominion.Toolkit.Rng;

public sealed record CorrectiveRngProvenance(
    int SchemaVersion,
    string EncodedGameId,
    int NativeSeed,
    ulong RootSeed,
    bool CorrectiveStateRequired);

public enum CorrectiveRngProvenanceValidation
{
    Valid,
    UnsupportedSchema,
    GameIdMismatch,
    NativeSeedMismatch,
    RootSeedMismatch
}

public enum CorrectiveRngAttachmentDecision
{
    LegacyWithoutCorrectiveHistory,
    RestoreCorrectiveSnapshot,
    CorrectiveSnapshotRequiredButMissing,
    CorrectiveSnapshotInvalid
}

public static class CorrectiveRngProvenanceCodec
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static CorrectiveRngProvenance MarkCorrective(
        string encodedGameId,
        int nativeSeed) =>
        new(
            CurrentSchemaVersion,
            RequireGameId(encodedGameId),
            nativeSeed,
            NativeRunSeed.ToRootSeed(nativeSeed),
            true);

    public static string Serialize(CorrectiveRngProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        return JsonSerializer.Serialize(provenance, Options);
    }

    public static CorrectiveRngProvenance Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Provenance JSON cannot be null or whitespace.", nameof(json));

        return JsonSerializer.Deserialize<CorrectiveRngProvenance>(json, Options)
            ?? throw new InvalidOperationException("Corrective RNG provenance JSON produced no value.");
    }

    public static CorrectiveRngProvenanceValidation Validate(
        CorrectiveRngProvenance provenance,
        string encodedGameId,
        int nativeSeed)
    {
        ArgumentNullException.ThrowIfNull(provenance);

        if (provenance.SchemaVersion != CurrentSchemaVersion)
            return CorrectiveRngProvenanceValidation.UnsupportedSchema;
        if (!string.Equals(provenance.EncodedGameId, encodedGameId, StringComparison.Ordinal))
            return CorrectiveRngProvenanceValidation.GameIdMismatch;
        if (provenance.NativeSeed != nativeSeed)
            return CorrectiveRngProvenanceValidation.NativeSeedMismatch;
        if (provenance.RootSeed != NativeRunSeed.ToRootSeed(nativeSeed))
            return CorrectiveRngProvenanceValidation.RootSeedMismatch;

        return CorrectiveRngProvenanceValidation.Valid;
    }

    public static CorrectiveRngAttachmentDecision DecideAttachment(
        CorrectiveRngProvenance? provenance,
        CorrectiveRngProvenanceValidation provenanceValidation,
        CorrectiveRngSidecar? sidecar,
        CorrectiveRngSidecarValidation sidecarValidation)
    {
        if (provenance is null)
            return sidecar is null
                ? CorrectiveRngAttachmentDecision.LegacyWithoutCorrectiveHistory
                : CorrectiveRngAttachmentDecision.CorrectiveSnapshotInvalid;

        if (provenanceValidation != CorrectiveRngProvenanceValidation.Valid)
            return CorrectiveRngAttachmentDecision.CorrectiveSnapshotInvalid;

        if (!provenance.CorrectiveStateRequired)
            return CorrectiveRngAttachmentDecision.LegacyWithoutCorrectiveHistory;

        if (sidecar is null)
            return CorrectiveRngAttachmentDecision.CorrectiveSnapshotRequiredButMissing;

        return sidecarValidation == CorrectiveRngSidecarValidation.Valid
            ? CorrectiveRngAttachmentDecision.RestoreCorrectiveSnapshot
            : CorrectiveRngAttachmentDecision.CorrectiveSnapshotInvalid;
    }

    private static string RequireGameId(string encodedGameId)
    {
        if (string.IsNullOrWhiteSpace(encodedGameId))
            throw new ArgumentException("Encoded GAME ID cannot be null or whitespace.", nameof(encodedGameId));
        return encodedGameId;
    }
}
