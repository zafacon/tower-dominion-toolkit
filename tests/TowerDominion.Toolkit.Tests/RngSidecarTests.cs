using System.Text;
using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngSidecarTests
{
    private static readonly RngKey Identity = new("Run", "Neutral", "Identity");

    [Fact]
    public void SidecarRoundTripValidatesExactNativeSaveGeneration()
    {
        const int nativeSeed = -123456789;
        const string gameId = "encoded-game-id";
        byte[] save = Encoding.UTF8.GetBytes("native-save-generation-A");
        ulong root = NativeRunSeed.ToRootSeed(nativeSeed);
        var manager = new RngManager(root, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
        _ = manager.NextUInt64(Identity);

        CorrectiveRngSidecar created = CorrectiveRngSidecarCodec.Create(gameId, nativeSeed, save, manager.CaptureSnapshot());
        string json = CorrectiveRngSidecarCodec.Serialize(created);
        CorrectiveRngSidecar restored = CorrectiveRngSidecarCodec.Deserialize(json);

        Assert.Equal(CorrectiveRngSidecarValidation.Valid,
            CorrectiveRngSidecarCodec.Validate(restored, gameId, nativeSeed, save));
    }

    [Fact]
    public void DifferentNativeSaveBytesRejectStaleSidecar()
    {
        const int nativeSeed = 42;
        const string gameId = "same-run";
        ulong root = NativeRunSeed.ToRootSeed(nativeSeed);
        var manager = new RngManager(root, new Fnv1aKeyDeriver(), new Pcg32Factory());
        byte[] oldSave = Encoding.UTF8.GetBytes("old");
        byte[] newSave = Encoding.UTF8.GetBytes("new");

        CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Create(gameId, nativeSeed, oldSave, manager.CaptureSnapshot());

        Assert.Equal(CorrectiveRngSidecarValidation.NativeSaveMismatch,
            CorrectiveRngSidecarCodec.Validate(sidecar, gameId, nativeSeed, newSave));
    }

    [Theory]
    [InlineData("other-game-id", 42, CorrectiveRngSidecarValidation.GameIdMismatch)]
    [InlineData("game-id", 43, CorrectiveRngSidecarValidation.NativeSeedMismatch)]
    public void NativeIdentityMismatchIsRejected(string gameId, int nativeSeed, CorrectiveRngSidecarValidation expected)
    {
        byte[] save = Encoding.UTF8.GetBytes("save");
        var manager = new RngManager(42, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
        CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Create("game-id", 42, save, manager.CaptureSnapshot());

        Assert.Equal(expected, CorrectiveRngSidecarCodec.Validate(sidecar, gameId, nativeSeed, save));
    }

    [Fact]
    public void CreateRejectsSnapshotFromDifferentRoot()
    {
        var manager = new RngManager(99, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());

        Assert.Throws<ArgumentException>(() =>
            CorrectiveRngSidecarCodec.Create("game-id", 42, new byte[] { 1 }, manager.CaptureSnapshot()));
    }

    [Fact]
    public void CorruptHashIsRejectedWithoutThrowing()
    {
        byte[] save = Encoding.UTF8.GetBytes("save");
        var manager = new RngManager(42, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
        CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Create(
            "game-id",
            42,
            save,
            manager.CaptureSnapshot()) with
        { NativeSaveSha256 = "not-hex" };

        Assert.Equal(CorrectiveRngSidecarValidation.NativeSaveMismatch,
            CorrectiveRngSidecarCodec.Validate(sidecar, "game-id", 42, save));
    }

    [Fact]
    public void Sha256FingerprintIsStable()
    {
        byte[] save = Encoding.UTF8.GetBytes("Tower Dominion");

        Assert.Equal(
            "fd62bd17afd834fa6088e2864e4013d25609da1247261bb28ff5ee1c8c5a3427",
            CorrectiveRngSidecarCodec.ComputeSha256(save));
    }
}
