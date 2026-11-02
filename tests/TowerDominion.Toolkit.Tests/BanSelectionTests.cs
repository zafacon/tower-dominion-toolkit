using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class BanSelectionTests
{
    [Fact]
    public void PersistsIndependentCategoriesAndUnbans()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-bans-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            EntityCatalog catalog = new();
            catalog.Replace("foes", new[] { new CatalogEntity("foe-1", "Medacris", Array.Empty<string>()), new CatalogEntity("foe-2", "Other", Array.Empty<string>()) });
            catalog.Replace("waves", new[] { new CatalogEntity("wave-1", "Hell from Above", Array.Empty<string>()) });
            BanSelections selections = new(path);
            BanCommands commands = new(catalog, selections, () => true);
            Assert.Contains("Banned 1", commands.Execute("ban foes dacri"));
            Assert.Contains("Banned 1", commands.Execute("ban waves \"Hell from Above\""));
            Assert.True(new BanSelections(path).IsBanned("foes", "foe-1"));
            Assert.False(new BanSelections(path).IsBanned("waves", "foe-1"));
            Assert.Contains("Banned 1", commands.Execute("ban foes all"));
            Assert.Contains("Unbanned 2", commands.Execute("unban foes all"));
            Assert.False(new BanSelections(path).IsBanned("foes", "foe-1"));
            Assert.True(new BanSelections(path).IsBanned("waves", "wave-1"));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void InvalidNamesDoNotPartiallyChangeBans()
    {
        string path = Path.Combine(Path.GetTempPath(), "toolkit-bans-" + Guid.NewGuid().ToString("N") + ".json");
        EntityCatalog catalog = new();
        catalog.Replace("foes", new[] { new CatalogEntity("foe-1", "Medacris", Array.Empty<string>()) });
        BanSelections selections = new(path);
        BanCommands commands = new(catalog, selections, () => true);
        Assert.Contains("No bans changed", commands.Execute("ban foes Medacris,Unknown"));
        Assert.Empty(selections.List("foes"));
        Assert.Contains("not ready", new BanCommands(catalog, selections, () => false).Execute("ban foes all"));
    }
}
