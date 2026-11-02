using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class NamedBanlistTests
{
    [Fact]
    public void SaveLoadUnloadDeleteAndRestartPreserveActiveState()
    {
        string root = Path.Combine(Path.GetTempPath(), "toolkit-banlists-" + Guid.NewGuid().ToString("N"));
        try
        {
            EntityCatalog catalog = new();
            catalog.Replace("foes", new[] { new CatalogEntity("foe-1", "Medacris", Array.Empty<string>()) });
            catalog.Replace("waves", new[] { new CatalogEntity("FW01", "Hell from Above", Array.Empty<string>()) });
            string path = Path.Combine(root, "bans.json");
            BanSelections active = new(path);
            NamedBanlists presets = new(Path.Combine(root, "Banlists"), active, catalog);
            active.Change("foes", new List<string> { "foe-1" }, true);
            Assert.Contains("Saved", presets.Execute("save banned as Test One"));
            Assert.Contains("Test One", presets.Execute("list banlists"));
            Assert.Contains("Medacris", presets.Execute("list banlist Test One"));
            active.Change("foes", new List<string> { "foe-1" }, false);
            active.Change("waves", new List<string> { "FW01" }, true);
            Assert.Contains("Loaded", presets.Execute("load banlist Test One"));
            Assert.True(new BanSelections(path).IsBanned("foes", "foe-1"));
            Assert.False(new BanSelections(path).IsBanned("waves", "FW01"));
            Assert.Contains("Unloaded", presets.Execute("unload banlist Test One"));
            Assert.False(active.IsBanned("foes", "foe-1"));
            Assert.Contains("Deleted", presets.Execute("delete banlist Test One"));
            Assert.Empty(presets.Names());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void InvalidNamesCannotEscapePresetDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), "toolkit-banlists-" + Guid.NewGuid().ToString("N"));
        try
        {
            NamedBanlists presets = new(Path.Combine(root, "Banlists"), new BanSelections(Path.Combine(root, "bans.json")), new EntityCatalog());
            Assert.Throws<ArgumentException>(() => presets.Save("../outside"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
