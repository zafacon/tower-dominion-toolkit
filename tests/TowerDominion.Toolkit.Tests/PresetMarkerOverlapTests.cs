using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PresetMarkerOverlapTests
{
    [Theory]
    [InlineData("list", "presets", false)]
    [InlineData("inspect", "preset one", true)]
    [InlineData("delete", "preset one", true)]
    [InlineData("save", "as one", true)]
    public void PresetMarkerConflictsWithOrdinaryGreedyArgument(string verb, string value, bool needsName)
    {
        var grammar = Create();
        grammar.Register(new(verb, "frontiers", "Ordinary",
            [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.FreeForm)]));
        Assert.StartsWith(needsName ? (verb == "save" ? "as " : "preset ") : "presets", value);
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new(verb, "frontiers", "Preset",
            needsName ? [new ArgumentDefinition("name", "Name", Completion: ArgumentCompletion.Preset)] : [],
            IsPresetOperation: true, Variant: "preset")));
    }

    [Fact]
    public void PresetListMarkerConflictsWithSingleOrdinaryArgument()
    {
        var grammar = Create();
        grammar.Register(new("list", "frontiers", "Ordinary",
            [new ArgumentDefinition("category", "Category")]));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("list", "frontiers", "Presets", [],
            IsPresetOperation: true, Variant: "presets")));
    }

    [Fact]
    public void DisjointAllowedValueDoesNotConflictWithPresetMarker()
    {
        var grammar = Create();
        grammar.Register(new("list", "frontiers", "Ordinary",
            [new ArgumentDefinition("category", "Category", AllowedValues: ["challenges"])]));
        grammar.Register(new("list", "frontiers", "Presets", [],
            IsPresetOperation: true, Variant: "presets"));
        Assert.Equal(2, grammar.Operations.Count);
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        return grammar;
    }
}
