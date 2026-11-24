using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Tests;

public sealed class RngPersistenceStoreTests
{
    [Fact]
    public void PathsAreDerivedBesideNativeProgressSave()
    {
        var paths = CorrectiveRngPersistencePaths.BesideNativeSave("/save/Progress.gd");

        Assert.Equal("/save/Progress.gd", paths.NativeSavePath);
        Assert.Equal("/save/Progress.gd.tdtoolkit-rng.json", paths.SidecarPath);
        Assert.Equal("/save/Progress.gd.tdtoolkit-rng.provenance.json", paths.ProvenancePath);
    }

    [Fact]
    public void AtomicWriteReplacesExistingContentWithoutLeavingTemporaryFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-rng-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "state.json");
        try
        {
            var store = new FileCorrectiveRngPersistenceStore();
            store.WriteTextAtomic(path, "first");
            store.WriteTextAtomic(path, "second");

            Assert.Equal("second", store.ReadText(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
