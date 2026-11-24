using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngProvenanceTests
{
    [Fact]
    public void NoMarkerAndNoSidecarIsLegacy()
    {
        Assert.Equal(
            CorrectiveRngAttachmentDecision.LegacyWithoutCorrectiveHistory,
            CorrectiveRngProvenanceCodec.DecideAttachment(
                null,
                CorrectiveRngProvenanceValidation.Valid,
                null,
                CorrectiveRngSidecarValidation.Valid));
    }

    [Fact]
    public void CorrectiveMarkerRequiresSidecar()
    {
        CorrectiveRngProvenance provenance = CorrectiveRngProvenanceCodec.MarkCorrective("game", 42);

        Assert.Equal(
            CorrectiveRngAttachmentDecision.CorrectiveSnapshotRequiredButMissing,
            CorrectiveRngProvenanceCodec.DecideAttachment(
                provenance,
                CorrectiveRngProvenanceValidation.Valid,
                null,
                CorrectiveRngSidecarValidation.Valid));
    }

    [Fact]
    public void CorrectiveMarkerNeverTreatsInvalidSidecarAsLegacy()
    {
        CorrectiveRngProvenance provenance = CorrectiveRngProvenanceCodec.MarkCorrective("game", 42);
        var manager = new RngManager(42, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
        CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Create(
            "game", 42, new byte[] { 1 }, manager.CaptureSnapshot());

        Assert.Equal(
            CorrectiveRngAttachmentDecision.CorrectiveSnapshotInvalid,
            CorrectiveRngProvenanceCodec.DecideAttachment(
                provenance,
                CorrectiveRngProvenanceValidation.Valid,
                sidecar,
                CorrectiveRngSidecarValidation.NativeSaveMismatch));
    }

    [Fact]
    public void ValidMarkerAndSidecarRestores()
    {
        CorrectiveRngProvenance provenance = CorrectiveRngProvenanceCodec.MarkCorrective("game", 42);
        var manager = new RngManager(42, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
        CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Create(
            "game", 42, new byte[] { 1 }, manager.CaptureSnapshot());

        Assert.Equal(
            CorrectiveRngAttachmentDecision.RestoreCorrectiveSnapshot,
            CorrectiveRngProvenanceCodec.DecideAttachment(
                provenance,
                CorrectiveRngProvenanceValidation.Valid,
                sidecar,
                CorrectiveRngSidecarValidation.Valid));
    }

    [Fact]
    public void ProvenanceRoundTripPreservesRunIdentity()
    {
        CorrectiveRngProvenance source = CorrectiveRngProvenanceCodec.MarkCorrective("encoded", -1);
        CorrectiveRngProvenance restored =
            CorrectiveRngProvenanceCodec.Deserialize(CorrectiveRngProvenanceCodec.Serialize(source));

        Assert.Equal(source, restored);
        Assert.Equal(
            CorrectiveRngProvenanceValidation.Valid,
            CorrectiveRngProvenanceCodec.Validate(restored, "encoded", -1));
    }

    [Theory]
    [InlineData("other", 42, CorrectiveRngProvenanceValidation.GameIdMismatch)]
    [InlineData("game", 43, CorrectiveRngProvenanceValidation.NativeSeedMismatch)]
    public void WrongRunIdentityRejectsMarker(
        string encodedGameId,
        int nativeSeed,
        CorrectiveRngProvenanceValidation expected)
    {
        CorrectiveRngProvenance provenance = CorrectiveRngProvenanceCodec.MarkCorrective("game", 42);

        Assert.Equal(expected, CorrectiveRngProvenanceCodec.Validate(provenance, encodedGameId, nativeSeed));
    }
}
