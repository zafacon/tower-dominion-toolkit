using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StartingTilePreviewIntegrationTests
{
    [Fact]
    public void MutationsAndPresetLoadsReturnCurrentSnapshotsWithoutSharingMutableArrays()
    {
        string root = Path.Combine(Path.GetTempPath(), "td-preview-" + Guid.NewGuid());
        try
        {
            var tiles = new StartingTilePresets(Path.Combine(root, "tiles"));
            var grammar = new CommandGrammar();
            StartingTileCommandRegistration.Register(grammar, tiles);
            var selections = new SelectionPresetAdapters(Path.Combine(root, "selections"),
                new BanSelections(Path.Combine(root, "bans.json")),
                new FrontierSelections(Path.Combine(root, "frontiers.json")));
            var challenges = new ChallengeRegistry();
            PresetCommandRegistration.Register(grammar, selections,
                new ChallengePresetAdapter(Path.Combine(root, "challenges"), challenges,
                    new ChallengeSelections(Path.Combine(root, "challenges.json"), challenges)),
                new StartingTilePresetAdapter(tiles));
            var parser = new CommandTextParser(grammar);
            var set = parser.Execute("set starting tiles S");
            Assert.True(set.Success);
            var snapshot = Assert.IsType<Dictionary<string, string[]>>(set.Details.Preview!.Data["relative"]);
            Assert.Equal(["S"], snapshot["D1"]);
            Assert.True(parser.Execute("save starting tiles as Sample").Success);
            Assert.True(parser.Execute("clear starting tiles").Success);
            Assert.Empty(Assert.IsType<Dictionary<string, string[]>>(
                parser.Execute("inspect starting tiles").Details.Preview!.Data["relative"]));
            Assert.Equal(["S"], snapshot["D1"]);
            var loaded = parser.Execute("load starting tiles Sample");
            Assert.True(loaded.Success);
            Assert.Equal(["S"], Assert.IsType<Dictionary<string, string[]>>(loaded.Details.Preview!.Data["relative"])["D1"]);
            Assert.False(parser.Execute("set starting tiles invalid").Success);
            Assert.Null(parser.Execute("set starting tiles invalid").Details.Preview);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
