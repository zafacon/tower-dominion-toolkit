using System.Text;
using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngAttachmentCoordinatorTests
{
    [Fact]
    public void LegacySaveAttachesWithoutCreatingCorrectivePersistence()
    {
        var fixture = new Fixture();
        fixture.Observe();
        fixture.Store.Bytes[fixture.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("native");

        CorrectiveRngAttachmentResult result = fixture.Coordinator.Attach(fixture.Paths);

        Assert.Equal(CorrectiveRngAttachmentStatus.LegacyAttached, result.Status);
        Assert.Equal(CorrectiveRngLoadMode.ExistingRunWithoutSnapshot, result.Run!.LoadMode);
        Assert.Equal(CorrectiveRngLifecyclePhase.Attached, fixture.Lifecycle.Phase);
        Assert.Empty(fixture.Store.Writes);
    }

    [Fact]
    public void ValidCorrectiveStateRestoresExactContinuation()
    {
        var fixture = new Fixture();
        fixture.Observe();
        byte[] native = Encoding.UTF8.GetBytes("native");
        fixture.Store.Bytes[fixture.Paths.NativeSavePath] = native;

        var manager = new RngManager(42, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
        RngStream stream = manager.Resolve(RngCatalog.Neutral.Identity);
        _ = stream.NextUInt64();
        RngSnapshot snapshot = manager.CaptureSnapshot();
        ulong expected = stream.NextUInt64();

        CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Create("game", 42, native, snapshot);
        CorrectiveRngProvenance provenance = CorrectiveRngProvenanceCodec.MarkCorrective("game", 42);
        fixture.Store.Text[fixture.Paths.SidecarPath] = CorrectiveRngSidecarCodec.Serialize(sidecar);
        fixture.Store.Text[fixture.Paths.ProvenancePath] = CorrectiveRngProvenanceCodec.Serialize(provenance);

        CorrectiveRngAttachmentResult result = fixture.Coordinator.Attach(fixture.Paths);

        Assert.Equal(CorrectiveRngAttachmentStatus.CorrectiveRestored, result.Status);
        Assert.Equal(expected, result.Run!.Manager.Resolve(RngCatalog.Neutral.Identity).NextUInt64());
    }

    [Fact]
    public void CorrectiveMarkerWithoutSnapshotFailsClosedAndRemainsUnattached()
    {
        var fixture = new Fixture();
        fixture.Observe();
        fixture.Store.Bytes[fixture.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("native");
        fixture.Store.Text[fixture.Paths.ProvenancePath] =
            CorrectiveRngProvenanceCodec.Serialize(CorrectiveRngProvenanceCodec.MarkCorrective("game", 42));

        CorrectiveRngAttachmentResult result = fixture.Coordinator.Attach(fixture.Paths);

        Assert.Equal(CorrectiveRngAttachmentStatus.BlockedMissingSnapshot, result.Status);
        Assert.False(result.IsAttached);
        Assert.Equal(CorrectiveRngLifecyclePhase.RootObserved, fixture.Lifecycle.Phase);
    }

    [Fact]
    public void StaleSidecarFailsClosed()
    {
        var fixture = new Fixture();
        fixture.Observe();
        fixture.Store.Bytes[fixture.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("new native");

        var manager = new RngManager(42, new Fnv1aKeyDeriver(), new Xoshiro256StarStarFactory());
        var sidecar = CorrectiveRngSidecarCodec.Create(
            "game", 42, Encoding.UTF8.GetBytes("old native"), manager.CaptureSnapshot());
        fixture.Store.Text[fixture.Paths.SidecarPath] = CorrectiveRngSidecarCodec.Serialize(sidecar);
        fixture.Store.Text[fixture.Paths.ProvenancePath] =
            CorrectiveRngProvenanceCodec.Serialize(CorrectiveRngProvenanceCodec.MarkCorrective("game", 42));

        CorrectiveRngAttachmentResult result = fixture.Coordinator.Attach(fixture.Paths);

        Assert.Equal(CorrectiveRngAttachmentStatus.BlockedInvalidState, result.Status);
        Assert.False(result.IsAttached);
    }

    [Fact]
    public void CorruptPersistenceFailsClosedWithoutThrowing()
    {
        var fixture = new Fixture();
        fixture.Observe();
        fixture.Store.Bytes[fixture.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("native");
        fixture.Store.Text[fixture.Paths.ProvenancePath] = "{not json";

        CorrectiveRngAttachmentResult result = fixture.Coordinator.Attach(fixture.Paths);

        Assert.Equal(CorrectiveRngAttachmentStatus.BlockedInvalidState, result.Status);
        Assert.False(result.IsAttached);
    }

    private sealed class Fixture
    {
        public CorrectiveRngLifecycle Lifecycle { get; } =
            new(new CorrectiveRngSession(new Xoshiro256StarStarFactory(), new Fnv1aKeyDeriver()));
        public MemoryStore Store { get; } = new();
        public CorrectiveRngPersistencePaths Paths { get; } =
            CorrectiveRngPersistencePaths.BesideNativeSave("/save/Progress.gd");
        public CorrectiveRngAttachmentCoordinator Coordinator { get; }

        public Fixture() => Coordinator = new(Lifecycle, Store);
        public void Observe() => Lifecycle.ObserveRoot("game", 42);
    }

    private sealed class MemoryStore : ICorrectiveRngPersistenceStore
    {
        public Dictionary<string, byte[]> Bytes { get; } = new();
        public Dictionary<string, string> Text { get; } = new();
        public List<string> Writes { get; } = new();

        public bool Exists(string path) => Bytes.ContainsKey(path) || Text.ContainsKey(path);
        public byte[] ReadBytes(string path) => Bytes[path];
        public string ReadText(string path) => Text[path];
        public void WriteTextAtomic(string path, string content)
        {
            Writes.Add(path);
            Text[path] = content;
        }
        public void DeleteIfExists(string path)
        {
            Bytes.Remove(path);
            Text.Remove(path);
        }
    }
}
