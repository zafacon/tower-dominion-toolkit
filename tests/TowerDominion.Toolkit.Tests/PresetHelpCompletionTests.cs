using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PresetHelpCompletionTests
{
    [Fact]
    public void AllPresetOperationsAppearInHelp()
    {
        var grammar = Build(out _);
        var help = new CommandHelp(grammar);
        foreach (string domain in new[] { "bans", "frontiers", "challenges", "starting tiles" })
        {
            var syntax = help.Query(domain).Syntax;
            Assert.Contains($"save {domain} as <name>", syntax);
            Assert.Contains($"load {domain} <name>", syntax);
            Assert.Contains($"list {domain} presets", syntax);
            Assert.Contains($"inspect {domain} preset <name>", syntax);
            Assert.Contains($"delete {domain} preset <name>", syntax);
        }
    }

    [Fact]
    public void CompletionUsesLivePresetNamesAndQuotesSpaces()
    {
        var grammar = Build(out var names);
        var completion = new CommandArgumentCompletion(grammar);
        Assert.Equal(ArgumentCompletionStatus.Available, completion.Complete("load bans ", 10).Status);
        Assert.Empty(completion.Complete("load bans ", 10).Completion.Candidates);
        names.Add("My Setup");
        foreach (string command in new[] { "load bans My", "inspect bans preset My", "delete bans preset My" })
        {
            var result = completion.Complete(command, command.Length);
            Assert.Equal(ArgumentCompletionStatus.Available, result.Status);
            Assert.EndsWith("\"My Setup\"", result.Completion.Text);
        }
        Assert.True(new CommandTextParser(grammar).Execute("load bans \"My Setup\"").Success);
        names.Clear();
        Assert.Empty(completion.Complete("load bans My", 12).Completion.Candidates);
    }

    private static CommandGrammar Build(out List<string> names)
    {
        names = new List<string>();
        var available = names;
        var grammar = new CommandGrammar();
        foreach (string domain in new[] { "bans", "frontiers", "challenges", "starting tiles" })
        {
            grammar.RegisterDomain(new(domain, domain));
            grammar.RegisterPresets(domain, new(
                _ => CommandOutcome.Ok("saved"), _ => CommandOutcome.Ok("loaded"),
                _ => CommandOutcome.Ok("listed"), _ => CommandOutcome.Ok("inspected"),
                _ => CommandOutcome.Ok("deleted")), () => available.ToArray());
        }
        return grammar;
    }
}
