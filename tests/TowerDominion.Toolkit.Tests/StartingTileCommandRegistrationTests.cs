using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StartingTileCommandRegistrationTests
{
    [Fact]
    public void RelativeConfigurationsSetInspectAndClearByDifficulty()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-tile-commands-" + Guid.NewGuid());
        try
        {
            var tiles = new StartingTilePresets(root);
            var grammar = new CommandGrammar();
            StartingTileCommandRegistration.Register(grammar, tiles);
            var parser = new CommandTextParser(grammar);
            Assert.Equal(4, grammar.Operations.Count);
            Assert.True(parser.Execute("set starting tiles L.C").Success);
            Assert.True(parser.Execute("set starting tiles S,T.A").Success);
            Assert.Equal(["L.C"], tiles.Relative[1]);
            Assert.Equal(["S", "T.A"], tiles.Relative[2]);
            var inspected = parser.Execute("inspect starting tiles");
            Assert.Contains("D2: S,T.A", inspected.Message);
            Assert.Equal("starting tiles", inspected.Details.Preview!.Kind);
            var relative = Assert.IsType<Dictionary<string, string[]>>(inspected.Details.Data!["relative"]);
            Assert.Equal(["S", "T.A"], relative["D2"]);
            Assert.Equal(["L.C"], relative["D1"]);
            Assert.Contains("D1: L.C", parser.Execute("list starting tiles").Message);
            Assert.True(parser.Execute("clear starting tiles D1").Success);
            Assert.False(tiles.Relative.ContainsKey(1));
            Assert.Single(new StartingTilePresets(root).Relative);
            Assert.True(parser.Execute("clear starting tiles").Success);
            Assert.Empty(tiles.Relative);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void InvalidShapesAndDifficultyDoNotReplaceExistingTiles()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-tile-commands-" + Guid.NewGuid());
        try
        {
            var tiles = new StartingTilePresets(root);
            var grammar = new CommandGrammar();
            StartingTileCommandRegistration.Register(grammar, tiles);
            var parser = new CommandTextParser(grammar);
            Assert.True(parser.Execute("set starting tiles S").Success);
            Assert.False(parser.Execute("set starting tiles S,invalid").Success);
            Assert.False(parser.Execute("set starting tiles N:S,E:S,S:S,W:S").Success);
            Assert.False(parser.Execute("clear starting tiles D5").Success);
            Assert.Equal(["S"], tiles.Relative[1]);
            Assert.Single(tiles.Relative);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
