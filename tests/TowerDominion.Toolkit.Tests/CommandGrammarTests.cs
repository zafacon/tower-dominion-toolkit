using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandGrammarTests
{
    [Fact]
    public void DiscoversOperationsByDomainVerbAndQualifier()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontiers", "Frontier modifiers"));
        grammar.Register(new("list", "frontiers", "All modifiers", Array.Empty<ArgumentDefinition>()));
        grammar.Register(new("list", "frontiers", "Selected modifiers", Array.Empty<ArgumentDefinition>(), "selected"));
        grammar.Register(new("select", "frontiers", "Select modifiers",
            new[] { new ArgumentDefinition("names", "Modifier names", Completion: ArgumentCompletion.Catalog) }));
        Assert.True(grammar.Supports("LIST", "FRONTIERS", "SELECTED"));
        Assert.False(grammar.Supports("list", "frontiers", "active"));
        Assert.Equal(3, grammar.ForDomain("frontiers").Count);
        Assert.Equal(2, grammar.ForVerb("list").Count);
        Assert.Equal(ArgumentCompletion.Catalog, grammar.Find("select", "frontiers")!.Arguments[0].Completion);
    }

    [Fact]
    public void RejectsConflictingDefinitions()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select", Array.Empty<ArgumentDefinition>()));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("SELECT", "FRONTIERS", "Duplicate", Array.Empty<ArgumentDefinition>())));
        Assert.Throws<InvalidOperationException>(() => grammar.RegisterDomain(new("select", "Conflict")));
        Assert.Throws<InvalidOperationException>(() => grammar.Register(new("list", "missing", "Missing domain", Array.Empty<ArgumentDefinition>())));
        Assert.Throws<ArgumentException>(() => grammar.RegisterDomain(new("bad  name", "Invalid spacing")));
    }

    [Fact]
    public void SupportsPresetCapabilityMetadata()
    {
        CommandGrammar grammar = new();
        grammar.RegisterDomain(new("starting tiles", "Relative starting tiles"));
        grammar.Register(new("load", "starting tiles", "Replace configuration",
            new[] { new ArgumentDefinition("name", "Preset name", Completion: ArgumentCompletion.Preset) },
            IsPresetOperation: true));
        Assert.True(grammar.Find("load", "starting tiles")!.IsPresetOperation);
        Assert.True(grammar.Supports("load", "starting tiles"));
    }
}
