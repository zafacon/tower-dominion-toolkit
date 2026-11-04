using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PresetGrammarTests
{
    private static readonly string[] testName = ["test"];
    private static readonly string[] oneName = ["one"];

    [Fact]
    public void RegistersAllFivePresetOperationsWithoutCollidingWithOrdinaryList()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontiers", "Frontier modifiers"));
        grammar.Register(new("list", "frontiers", "Catalog", Array.Empty<ArgumentDefinition>(),
            Handler: _ => CommandOutcome.Ok("catalog")));
        var stored = new Dictionary<string, string>();
        grammar.RegisterPresets("frontiers", new(
            Save: i => { stored[i.Arguments[0]] = "selected"; return CommandOutcome.Ok("saved"); },
            Load: i => stored.TryGetValue(i.Arguments[0], out string? value) ? CommandOutcome.Ok(value) : CommandOutcome.Failure("missing"),
            List: _ => CommandOutcome.Ok(string.Join(",", stored.Keys)),
            Inspect: i => CommandOutcome.Ok(stored[i.Arguments[0]]),
            Delete: i => CommandOutcome.Ok(stored.Remove(i.Arguments[0]) ? "deleted" : "missing")));
        Assert.Equal(6, grammar.ForDomain("frontiers").Count);
        Assert.True(grammar.Supports("list", "frontiers"));
        Assert.True(grammar.Supports("list", "frontiers", variant: "presets"));
        Assert.Equal("catalog", grammar.Dispatch(new("list", "frontiers", Array.Empty<string>())).Message);
        Assert.True(grammar.Dispatch(new("save", "frontiers", testName, Variant: "preset")).Success);
        Assert.Equal("test", grammar.Dispatch(new("list", "frontiers", Array.Empty<string>(), Variant: "presets")).Message);
        Assert.Equal("selected", grammar.Dispatch(new("load", "frontiers", testName, Variant: "preset")).Message);
        Assert.True(grammar.Dispatch(new("delete", "frontiers", testName, Variant: "preset")).Success);
        Assert.Throws<InvalidOperationException>(() => grammar.RegisterPresets("frontiers", new(
            _ => CommandOutcome.Ok(""), _ => CommandOutcome.Ok(""), _ => CommandOutcome.Ok(""),
            _ => CommandOutcome.Ok(""), _ => CommandOutcome.Ok(""))));
    }

    [Fact]
    public void RejectsUnsupportedAndIncompleteInvocationsWithoutExecutingHandler()
    {
        int calls = 0;
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("challenges", "Challenges"));
        grammar.Register(new("select", "challenges", "Select",
            new[] { new ArgumentDefinition("ids", "Challenge IDs") },
            Handler: _ => { calls++; return CommandOutcome.Ok("selected"); }));
        Assert.False(grammar.Dispatch(new("select", "challenges", Array.Empty<string>())).Success);
        Assert.False(grammar.Dispatch(new("ban", "challenges", oneName)).Success);
        Assert.Equal(0, calls);
        Assert.True(grammar.Dispatch(new("select", "challenges", oneName)).Success);
        Assert.Equal(1, calls);
    }
}
