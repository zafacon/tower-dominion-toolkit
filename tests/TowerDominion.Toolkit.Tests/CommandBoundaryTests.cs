using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandBoundaryTests
{
    private static readonly string[] states = ["on", "off"];

    [Theory]
    [InlineData("set frontier diagnostics on extra")]
    [InlineData("set frontier diagnostics \"on extra\"")]
    [InlineData("set frontier diagnostics \"\"")]
    public void StrictLiteralBoundariesRejectSurplusOrEmpty(string command)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier diagnostics", "Diagnostics"));
        int calls = 0;
        grammar.Register(new("set", "frontier diagnostics", "State",
            [new ArgumentDefinition("state", "State", AllowedValues: states)],
            Handler: _ => { calls++; return CommandOutcome.Ok("ok"); }));
        Assert.False(new CommandTextParser(grammar).Execute(command).Success);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("select frontiers all", true, "")]
    [InlineData("select frontiers ALL", true, "")]
    [InlineData("select frontiers \"all\"", false, "all")]
    [InlineData("select frontiers \"First, Second\",Third", false, "First, Second|Third")]
    [InlineData("select frontiers \"a\\\"b\",c", false, "a\"b|c")]
    public void DistinguishesReservedAllFromLiteralMembers(string command, bool all, string members)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true)],
            Handler: i => CommandOutcome.Ok($"{i.AllSelections!.Contains("members")}|{string.Join("|", i.SelectionMembers!.GetValueOrDefault("members") ?? [])}")));
        Assert.Equal($"{all}|{members}", new CommandTextParser(grammar).Execute(command).Message);
    }

    [Fact]
    public void OverlapFailureDoesNotChangeRegistry()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("load", "frontiers", "Load", [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.Preset)]));
        int before = grammar.Operations.Count;
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("load", "frontiers", "Optional",
            [new ArgumentDefinition("name", "Name", Required: false)], Variant: "other")));
        Assert.Equal(before, grammar.Operations.Count);
    }

    [Fact]
    public void DomainPrefixCollisionIsRejected()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontier", "A"));
        grammar.RegisterDomain(new("frontier diagnostics", "B"));
        grammar.Register(new("set", "frontier", "Free", [new ArgumentDefinition("value", "Value", Completion: ArgumentCompletion.FreeForm)]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("set", "frontier diagnostics", "State",
            [new ArgumentDefinition("value", "Value")])));
    }
}

public sealed class PatternIntersectionTests
{
    private static readonly string[] on = ["on"];
    private static readonly string[] off = ["off"];

    [Fact]
    public void DisjointLiteralArgumentsMaySharePrefix()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("set", "seed", "On", [new ArgumentDefinition("state", "State", AllowedValues: on)]));
        grammar.Register(new("set", "seed", "Off", [new ArgumentDefinition("state", "State", AllowedValues: off)], Variant: "off"));
        Assert.Equal(2, grammar.Operations.Count);
    }

    [Fact]
    public void OptionalArgumentOverlapsZeroArgumentPattern()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("clear", "seed", "All", []));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("clear", "seed", "Optional",
            [new ArgumentDefinition("scope", "Scope", Required: false)], Variant: "scoped")));
        Assert.Single(grammar.Operations);
    }

    [Fact]
    public void PresetMarkerCollidesWithFreeFormOrdinaryArgument()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("list", "frontiers", "Filter",
            [new ArgumentDefinition("filter", "Filter", Completion: ArgumentCompletion.FreeForm)]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("list", "frontiers", "Presets", [],
            IsPresetOperation: true, Variant: "presets")));
    }
}
