using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class BlueprintBagPersistenceTests : IDisposable
{
    private static readonly string[] DuplicateIds = { "b", "a", "A" };
    private static readonly string[] DistinctIds = { "a", "b" };
    private static readonly string[] FirstId = { "a" };
    private static readonly string[] SecondId = { "b" };
    private readonly string directory = Path.Combine(Path.GetTempPath(), "td-bag-" + Guid.NewGuid());
    private string Config => Path.Combine(directory, "qol.json");
    private string Native => Path.Combine(directory, "Progress.gd");
    public BlueprintBagPersistenceTests()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Native, "native save");
    }
    public void Dispose() => Directory.Delete(directory, true);

    [Fact]
    public void DefaultsOffAndPersistsPreference()
    {
        var bag = new BlueprintBagPersistence(Config, Native);
        Assert.False(bag.Enabled);
        bag.SetEnabled(true);
        Assert.True(new BlueprintBagPersistence(Config, Native).Enabled);
        bag.SetEnabled(false);
        Assert.False(new BlueprintBagPersistence(Config, Native).Enabled);
    }

    [Fact]
    public void RestoresDistinctIdsWithoutModifyingNativeSave()
    {
        var bag = new BlueprintBagPersistence(Config, Native);
        bag.SetEnabled(true);
        bag.Save(DuplicateIds);
        Assert.Equal(DistinctIds, new BlueprintBagPersistence(Config, Native).Load());
        Assert.Equal("native save", File.ReadAllText(Native));
    }

    [Fact]
    public void ChangedNativeSaveRejectsStaleSidecar()
    {
        var bag = new BlueprintBagPersistence(Config, Native);
        bag.SetEnabled(true);
        bag.Save(FirstId);
        File.WriteAllText(Native, "new run");
        Assert.Empty(bag.Load());
        bag.Save(SecondId);
        Assert.Equal(SecondId, bag.Load());
    }

    [Fact]
    public void ClearAndDisableDoNotModifyNativeSave()
    {
        var bag = new BlueprintBagPersistence(Config, Native);
        bag.SetEnabled(true);
        bag.Save(FirstId);
        bag.SetEnabled(false);
        Assert.Empty(bag.Load());
        bag.Clear();
        Assert.False(File.Exists(bag.SidecarPath));
        Assert.Equal("native save", File.ReadAllText(Native));
    }
}
