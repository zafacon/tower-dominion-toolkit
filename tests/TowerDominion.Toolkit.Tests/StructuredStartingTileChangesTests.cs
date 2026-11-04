using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StructuredStartingTileChangesTests
{
    [Fact]
    public void SetClearAndNoopReportOnlyAffectedDifficulties()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-tiles-changes-" + Guid.NewGuid());
        try
        {
            var tiles = new StartingTilePresets(root);
            var grammar = new CommandGrammar();
            StartingTileCommandRegistration.Register(grammar, tiles);
            var parser = new CommandTextParser(grammar);
            var set = parser.Execute("set starting tiles S");
            Assert.True(set.Success);
            Assert.Equal("D1", set.Details.Changes![0].Property);
            Assert.Empty(Assert.IsType<string[]>(set.Details.Changes[0].Before));
            Assert.Equal(["S"], Assert.IsType<string[]>(set.Details.Changes[0].After));
            Assert.Empty(parser.Execute("set starting tiles S").Details.Changes!);
            Assert.True(parser.Execute("set starting tiles S,T.A").Success);
            var clearOne = parser.Execute("clear starting tiles D1");
            Assert.True(clearOne.Success);
            Assert.Single(clearOne.Details.Changes!);
            Assert.Equal("D1", clearOne.Details.Changes![0].Property);
            Assert.Equal(["S"], Assert.IsType<string[]>(clearOne.Details.Changes[0].Before));
            Assert.Empty(Assert.IsType<string[]>(clearOne.Details.Changes[0].After));
            Assert.Empty(parser.Execute("clear starting tiles D1").Details.Changes!);
            var clearAll = parser.Execute("clear starting tiles");
            Assert.True(clearAll.Success);
            Assert.Single(clearAll.Details.Changes!);
            Assert.Equal("D2", clearAll.Details.Changes![0].Property);
            Assert.Empty(parser.Execute("clear starting tiles").Details.Changes!);
            var invalid = parser.Execute("set starting tiles invalid");
            Assert.False(invalid.Success);
            Assert.Null(invalid.Details.Changes);
            Assert.Empty(tiles.Relative);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
