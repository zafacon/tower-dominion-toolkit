using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class SelectionPresetAdaptersTests
{
    [Fact]
    public void BanPresetReplacesEveryCategoryRatherThanMerging()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-adapter-" + Guid.NewGuid());
        try
        {
            var bans = new BanSelections("unused", persist: false);
            var frontiers = new FrontierSelections(Path.Combine(directory, "selected.json"));
            var adapter = new SelectionPresetAdapters(directory, bans, frontiers);
            bans.Change("foes", ["one"], true);
            adapter.Save("bans", "Original");
            bans.Change("foes", ["two"], true);
            bans.Change("waves", ["wave"], true);
            adapter.Load("bans", "Original");
            Assert.Equal(["one"], bans.List("foes"));
            Assert.Empty(bans.List("waves"));
            Assert.Contains("foes: one", adapter.Inspect("bans", "Original"));
            Assert.Equal(["Original"], adapter.Names("bans"));
            Assert.True(adapter.Delete("bans", "Original"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void FrontierPresetReplacesSelectionAndPersistsAcrossReload()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-adapter-" + Guid.NewGuid());
        try
        {
            var bans = new BanSelections("unused", persist: false);
            string selectionPath = Path.Combine(directory, "selected.json");
            var frontiers = new FrontierSelections(selectionPath);
            var adapter = new SelectionPresetAdapters(directory, bans, frontiers);
            frontiers.Change(["one"], true);
            adapter.Save("frontiers", "One");
            frontiers.Change(["two"], true);
            adapter.Load("frontiers", "One");
            Assert.Equal(["one"], frontiers.SelectedIds);
            Assert.Equal(["one"], new FrontierSelections(selectionPath).SelectedIds);
            Assert.Equal("one", adapter.Inspect("frontiers", "One"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void InvalidPresetDoesNotChangeExistingSelections()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-adapter-" + Guid.NewGuid());
        try
        {
            var bans = new BanSelections("unused", persist: false);
            var frontiers = new FrontierSelections(Path.Combine(directory, "selected.json"));
            var adapter = new SelectionPresetAdapters(directory, bans, frontiers);
            frontiers.Change(["keep"], true);
            adapter.Save("frontiers", "Test");
            File.WriteAllText(Path.Combine(directory, "frontiers", "Test.json"), "[\"\"]");
            Assert.Throws<InvalidDataException>(() => adapter.Load("frontiers", "Test"));
            Assert.Equal(["keep"], frontiers.SelectedIds);
            bans.Change("foes", ["keep"], true);
            adapter.Save("bans", "Test");
            File.WriteAllText(Path.Combine(directory, "bans", "Test.json"), "{\"invalid\":[\"x\"]}");
            Assert.Throws<InvalidDataException>(() => adapter.Load("bans", "Test"));
            Assert.Equal(["keep"], bans.List("foes"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
