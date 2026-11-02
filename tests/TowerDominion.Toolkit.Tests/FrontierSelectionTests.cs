using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class FrontierSelectionTests
{
    [Fact]
    public void PersistsSelectionsAcrossInstancesAndSupportsAll()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-frontier-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            EntityCatalog catalog = new();
            catalog.Replace("frontiers", new[]
            {
                new CatalogEntity("one", "Feral world", Array.Empty<string>()),
                new CatalogEntity("two", "Frozen logistics", Array.Empty<string>())
            });
            FrontierSelections selections = new(path);
            FrontierCommands commands = new(catalog, selections, () => true);
            Assert.Contains("Selected 2", commands.Execute("select frontier \"Feral world\",\"Frozen logistics\""));
            Assert.Contains("2", commands.ListSelected("list selected-frontiers"));
            Assert.Contains("0 persistent", commands.Execute("select frontier all"));
            Assert.Contains("Unselected 2", commands.Execute("unselect frontier all"));
            Assert.Empty(new FrontierSelections(path).SelectedIds);
            Assert.Contains("Selected 1", commands.Execute("select frontier Feral world"));
            Assert.Contains("one", new FrontierSelections(path).SelectedIds);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void UnknownNamesCannotPartiallyMutateState()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-frontier-" + Guid.NewGuid().ToString("N") + ".json");
        EntityCatalog catalog = new();
        catalog.Replace("frontiers", new[] { new CatalogEntity("one", "Feral world", Array.Empty<string>()) });
        FrontierSelections selections = new(path);
        FrontierCommands commands = new(catalog, selections, () => true);
        Assert.Contains("No selections changed", commands.Execute("select frontier Feral world,Missing"));
        Assert.Empty(selections.SelectedIds);
        Assert.Contains("not ready", new FrontierCommands(catalog, selections, () => false).Execute("select frontier all"));
    }
}
