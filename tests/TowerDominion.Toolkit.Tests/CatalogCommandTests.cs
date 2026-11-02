using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CatalogCommandTests
{
    [Fact]
    public void ListReportsNotReadyWithoutPretendingCatalogIsEmpty()
    {
        EntityCatalog catalog = new();
        CatalogCommands commands = new(catalog, () => false);
        Assert.Contains("not ready", commands.Execute("list frontiers"));
    }

    [Fact]
    public void ListsRealSnapshotSortedWithoutExposingIds()
    {
        EntityCatalog catalog = new();
        catalog.Replace("frontiers", new[]
        {
            new CatalogEntity("two", "Zulu", Array.Empty<string>()),
            new CatalogEntity("one", "Alpha", Array.Empty<string>())
        });
        CatalogCommands commands = new(catalog, () => true);
        string output = commands.Execute("list frontiers");
        Assert.Contains("frontiers (2)", output);
        Assert.True(output.IndexOf("Alpha", StringComparison.Ordinal) < output.IndexOf("Zulu", StringComparison.Ordinal));
        Assert.DoesNotContain("[one]", output);
        Assert.Contains("Usage:", commands.Execute("list unknown"));
    }
}
