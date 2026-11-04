using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandSyntaxTests
{
    [Theory]
    [InlineData("save", "save frontiers as <name>")]
    [InlineData("load", "load frontiers <name>")]
    [InlineData("list", "list frontiers presets")]
    [InlineData("inspect", "inspect frontiers preset <name>")]
    [InlineData("delete", "delete frontiers preset <name>")]
    public void PresetSyntaxUsesRegisteredMarkers(string verb, string expected)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.RegisterPresets("frontiers", new(
            _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok"),
            _ => CommandOutcome.Ok("ok"), _ => CommandOutcome.Ok("ok"),
            _ => CommandOutcome.Ok("ok")));
        Assert.Contains(grammar.ForVerb(verb), operation => operation.Syntax == expected);
    }

    [Fact]
    public void SyntaxShowsQualifierLiteralsSelectionsAndOptionalArguments()
    {
        var operation = new OperationDefinition("clear", "starting tiles", "Clear",
            [new ArgumentDefinition("difficulty", "Difficulty", Required: false,
                AllowedValues: ["D1", "D2", "D3", "D4"])], Qualifier: "selected",
            Examples: ["clear selected starting tiles D3"], PersistenceDescription: "Persistent",
            DefaultDescription: "Native random selection");
        Assert.Equal("clear selected starting tiles [D1|D2|D3|D4]", operation.Syntax);
        Assert.Equal("Persistent", operation.PersistenceDescription);
        Assert.Equal("Native random selection", operation.DefaultDescription);
        Assert.Equal("clear selected starting tiles D3", Assert.Single(operation.Examples!));
        Assert.Equal("select frontiers <members|all>", new OperationDefinition("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true)]).Syntax);
    }
}
