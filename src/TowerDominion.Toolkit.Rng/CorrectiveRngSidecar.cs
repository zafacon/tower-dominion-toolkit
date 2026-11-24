using System.Security.Cryptography;
using System.Text.Json;

namespace TowerDominion.Toolkit.Rng;

public sealed record CorrectiveRngSidecar(
    int SchemaVersion,
    string EncodedGameId,
    int NativeSeed,
    ulong RootSeed,
    string NativeSaveSha256,
    RngSnapshot Snapshot);

public enum CorrectiveRngSidecarValidation
{
    Valid,
    UnsupportedSchema,
    GameIdMismatch,
    NativeSeedMismatch,
    RootSeedMismatch,
    NativeSaveMismatch,
    SnapshotRootMismatch
}

public static class CorrectiveRngSidecarCodec
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static CorrectiveRngSidecar Create(
        string encodedGameId,
        int nativeSeed,
        ReadOnlySpan<byte> nativeSaveBytes,
        RngSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(encodedGameId))
            throw new ArgumentException("Encoded GAME ID cannot be null or whitespace.", nameof(encodedGameId));
        ArgumentNullException.ThrowIfNull(snapshot);

        ulong rootSeed = NativeRunSeed.ToRootSeed(nativeSeed);
        if (snapshot.RootSeed != rootSeed)
            throw new ArgumentException("Snapshot root does not match the native run seed.", nameof(snapshot));

        return new(
            CurrentSchemaVersion,
            encodedGameId,
            nativeSeed,
            rootSeed,
            ComputeSha256(nativeSaveBytes),
            snapshot);
    }

    public static string Serialize(CorrectiveRngSidecar sidecar)
    {
        ArgumentNullException.ThrowIfNull(sidecar);
        return JsonSerializer.Serialize(sidecar, Options);
    }

    public static CorrectiveRngSidecar Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Sidecar JSON cannot be null or whitespace.", nameof(json));

        return JsonSerializer.Deserialize<CorrectiveRngSidecar>(json, Options)
            ?? throw new InvalidOperationException("Corrective RNG sidecar JSON produced no value.");
    }

    public static CorrectiveRngSidecarValidation Validate(
        CorrectiveRngSidecar sidecar,
        string encodedGameId,
        int nativeSeed,
        ReadOnlySpan<byte> nativeSaveBytes)
    {
        ArgumentNullException.ThrowIfNull(sidecar);

        if (sidecar.SchemaVersion != CurrentSchemaVersion)
            return CorrectiveRngSidecarValidation.UnsupportedSchema;
        if (!string.Equals(sidecar.EncodedGameId, encodedGameId, StringComparison.Ordinal))
            return CorrectiveRngSidecarValidation.GameIdMismatch;
        if (sidecar.NativeSeed != nativeSeed)
            return CorrectiveRngSidecarValidation.NativeSeedMismatch;

        ulong rootSeed = NativeRunSeed.ToRootSeed(nativeSeed);
        if (sidecar.RootSeed != rootSeed)
            return CorrectiveRngSidecarValidation.RootSeedMismatch;
        if (!FixedTimeEqualsHex(sidecar.NativeSaveSha256, ComputeSha256(nativeSaveBytes)))
            return CorrectiveRngSidecarValidation.NativeSaveMismatch;
        if (sidecar.Snapshot.RootSeed != rootSeed)
            return CorrectiveRngSidecarValidation.SnapshotRootMismatch;

        return CorrectiveRngSidecarValidation.Valid;
    }

    public static string ComputeSha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static bool FixedTimeEqualsHex(string left, string right)
    {
        if (left.Length != right.Length)
            return false;

        try
        {
            byte[] leftBytes = Convert.FromHexString(left);
            byte[] rightBytes = Convert.FromHexString(right);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
