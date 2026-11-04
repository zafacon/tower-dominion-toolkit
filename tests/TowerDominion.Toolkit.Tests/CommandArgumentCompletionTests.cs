using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandArgumentCompletionTests
{
    [Fact]
    public void AllowedValuesAreSuggested()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("set", "seed", "Mode", [new ArgumentDefinition("mode", "Mode",
            Completion: ArgumentCompletion.Literal, AllowedValues: ["fixed", "random"])]));
        var result = new CommandArgumentCompletion(grammar).Complete("set seed fi", 11);
        Assert.Equal("set seed fixed", result.Completion.Text);
        Assert.Equal(ArgumentCompletionStatus.Available, result.Status);
    }

    [Fact]
    public void CatalogIsReadOnEveryRequest()
    {
        var names = new List<string> { "Alpha" };
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("foes", "Foes"));
        grammar.Register(new("inspect", "foes", "Inspect", [new ArgumentDefinition("name", "Name",
            Completion: ArgumentCompletion.Catalog, CatalogProvider: () => names.ToArray())]));
        var service = new CommandArgumentCompletion(grammar);
        Assert.Equal("inspect foes Alpha", service.Complete("inspect foes Al", 15).Completion.Text);
        names.Clear();
        names.Add("Beta");
        Assert.Equal("inspect foes Beta", service.Complete("inspect foes Be", 15).Completion.Text);
    }

    [Fact]
    public void MissingCatalogDiffersFromEmptyCatalog()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("load", "frontiers", "Load", [new ArgumentDefinition("name", "Preset",
            Completion: ArgumentCompletion.Preset)]));
        Assert.Equal(ArgumentCompletionStatus.Unavailable,
            new CommandArgumentCompletion(grammar).Complete("load frontiers ", 15).Status);
    }

    [Fact]
    public void FreeFormIsUnrestrictedAndUnknownStructureIsNotArgument()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("set", "seed", "Set", [new ArgumentDefinition("value", "Value",
            Completion: ArgumentCompletion.FreeForm)]));
        var service = new CommandArgumentCompletion(grammar);
        Assert.Equal(ArgumentCompletionStatus.Unrestricted, service.Complete("set seed ", 9).Status);
        Assert.Equal(ArgumentCompletionStatus.NotArgument, service.Complete("set unknown ", 12).Status);
    }
}
