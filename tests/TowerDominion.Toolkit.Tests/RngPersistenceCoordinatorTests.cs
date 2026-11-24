using System.Text;
using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngPersistenceCoordinatorTests
{
    [Fact]
    public void ActivationWritesProvenanceBeforeSidecarAndBindsCurrentNativeSave()
    {
        var f = new Fixture();
        f.AttachLegacy();
        f.Store.Bytes[f.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("native");

        f.Coordinator.Activate(f.Paths);

        Assert.Equal(new[] { f.Paths.ProvenancePath, f.Paths.SidecarPath }, f.Store.Writes);
        var provenance = CorrectiveRngProvenanceCodec.Deserialize(f.Store.Text[f.Paths.ProvenancePath]);
        var sidecar = CorrectiveRngSidecarCodec.Deserialize(f.Store.Text[f.Paths.SidecarPath]);
        Assert.Equal(CorrectiveRngProvenanceValidation.Valid,
            CorrectiveRngProvenanceCodec.Validate(provenance, "game", 42));
        Assert.Equal(CorrectiveRngSidecarValidation.Valid,
            CorrectiveRngSidecarCodec.Validate(sidecar, "game", 42, f.Store.Bytes[f.Paths.NativeSavePath]));
    }

    [Fact]
    public void NativeSaveWithoutActivationDoesNotCreatePersistence()
    {
        var f = new Fixture();
        f.AttachLegacy();
        f.Store.Bytes[f.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("native");

        f.Coordinator.PersistAfterNativeSave(f.Paths);

        Assert.Empty(f.Store.Writes);
    }

    [Fact]
    public void SubsequentNativeSaveReplacesSidecarForNewGeneration()
    {
        var f = new Fixture();
        f.AttachLegacy();
        f.Store.Bytes[f.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("first");
        f.Coordinator.Activate(f.Paths);

        _ = f.Lifecycle.Current!.Manager.Resolve(RngCatalog.Neutral.Identity).NextUInt64();
        f.Store.Bytes[f.Paths.NativeSavePath] = Encoding.UTF8.GetBytes("second");
        f.Coordinator.PersistAfterNativeSave(f.Paths);

        var sidecar = CorrectiveRngSidecarCodec.Deserialize(f.Store.Text[f.Paths.SidecarPath]);
        Assert.Equal(CorrectiveRngSidecarValidation.Valid,
            CorrectiveRngSidecarCodec.Validate(sidecar, "game", 42, Encoding.UTF8.GetBytes("second")));
        Assert.Equal(3, f.Store.Writes.Count);
        Assert.Equal(f.Paths.SidecarPath, f.Store.Writes[^1]);
    }

    private sealed class Fixture
    {
        public CorrectiveRngLifecycle Lifecycle { get; } =
            new(new CorrectiveRngSession(new Xoshiro256StarStarFactory(), new Fnv1aKeyDeriver()));
        public MemoryStore Store { get; } = new();
        public CorrectiveRngPersistencePaths Paths { get; } =
            CorrectiveRngPersistencePaths.BesideNativeSave("/save/Progress.gd");
        public CorrectiveRngPersistenceCoordinator Coordinator { get; }

        public Fixture() => Coordinator = new(Lifecycle, Store);

        public void AttachLegacy()
        {
            Lifecycle.ObserveRoot("game", 42);
            Lifecycle.AttachLegacy();
        }
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
