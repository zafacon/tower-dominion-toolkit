using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandHelpTests
{
    [Fact]
    public void HelpTopicsExposeMultipleCommandForms()
    {
        CommandRegistry registry = new();
        registry.Register(new("set", "set old", "Old summary.", _ => "ok"));
        registry.Register(new("clear", "clear old", "Old summary.", _ => "ok"));
        registry.AddHelp("set", "set starting tiles <shapes>", "Relative coordinates.", "starting tiles");
        registry.AddHelp("set", "set relative tiles D3 <shapes>", "Explicit difficulty.", "starting tiles");
        registry.AddHelp("clear", "clear starting tiles 3", "Clear difficulty.", "starting tiles");

        Assert.Contains("set starting tiles <shapes>", registry.Execute("help set"));
        Assert.Contains("set relative tiles D3 <shapes>", registry.Execute("help starting tiles"));
        Assert.Contains("clear starting tiles 3", registry.Execute("help starting tiles"));
        registry.AddHelp("set", "set unrelated", "Other feature.", "unrelated");
        Assert.DoesNotContain("set unrelated", registry.Execute("help starting tiles"));
        Assert.DoesNotContain("set starting tiles", registry.Execute("help unrelated"));
        Assert.Contains("Topics: starting tiles", registry.Execute("help"));
        Assert.DoesNotContain("set old", registry.Execute("help set"));
    }

    [Fact]
    public void HelpFallsBackForCommandsWithoutDetailedEntries()
    {
        CommandRegistry registry = new();
        registry.Register(new("version", "version", "Show version.", _ => "ok"));
        Assert.Contains("Show version.", registry.Execute("help version"));
        Assert.Contains("Unknown command or help topic", registry.Execute("help missing"));
    }

    [Fact]
    public void RelativeStartingTilesUseEntryCountAndClearAliases()
    {
        string directory = Path.Combine(Path.GetTempPath(), "td-help-tiles-" + Guid.NewGuid());
        try
        {
            StartingTilePresets presets = new(directory);
            StartingTileCommands handler = new(presets);
            Assert.Contains("D3 starting tiles saved", handler.Execute("set starting tiles S,X,L.C"));
            Assert.Equal("S,X,L.C", string.Join(",", presets.Relative[3]));
            Assert.Contains("cleared", handler.Execute("clear starting tiles 3"));
            Assert.False(presets.Relative.ContainsKey(3));
            Assert.Contains("D4 relative tiles saved", handler.Execute("set relative tiles D4 S,X,L.C,T.A"));
            Assert.Contains("cleared", handler.Execute("clear relative tiles D4"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
