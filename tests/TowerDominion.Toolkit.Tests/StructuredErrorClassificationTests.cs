using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class StructuredErrorClassificationTests
{
    [Fact]
    public void ParserClassifiesSyntaxAndArgumentValidation()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("set", "seed", "Set", [new ArgumentDefinition("value", "Value",
            AllowedValues: ["alpha"])], Handler: _ => CommandOutcome.Ok("done")));
        var parser = new CommandTextParser(grammar);
        Assert.Equal(CommandErrorKind.Syntax, parser.Execute("unknown seed").Details.Error);
        Assert.Equal(CommandErrorKind.Validation, parser.Execute("set seed beta").Details.Error);
        Assert.Equal(CommandErrorKind.Validation, grammar.Dispatch(new("set", "seed", ["beta"])).Details.Error);
        Assert.Equal(CommandErrorKind.Syntax, grammar.Dispatch(new("clear", "seed", [])).Details.Error);
    }

    [Fact]
    public void CustomValidationMessagesDoNotDetermineErrorKind()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("seed", "Seed"));
        grammar.Register(new("set", "seed", "Set", [new ArgumentDefinition("value", "Value",
            Validator: _ => "A custom rejection without a classification prefix.")],
            Handler: _ => CommandOutcome.Ok("done")));
        var parser = new CommandTextParser(grammar);
        var parsed = parser.Parse("set seed any");
        Assert.Equal(CommandErrorKind.Validation, parsed.ErrorKind);
        Assert.Equal(CommandErrorKind.Validation, parser.Execute("set seed any").Details.Error);
        Assert.Equal(CommandErrorKind.Validation, grammar.Dispatch(new("set", "seed", ["any"])).Details.Error);
    }

    [Fact]
    public void DispatchClassifiesUnavailableButPreservesHandlerErrors()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("list", "frontiers", "List", [], Handler: null));
        grammar.Register(new("set", "frontiers", "Set", [new ArgumentDefinition("name", "Name",
            CatalogProvider: () => null)], Handler: _ => CommandOutcome.Ok("done")));
        grammar.Register(new("clear", "frontiers", "Clear", [],
            Handler: _ => CommandOutcome.Failure("domain failure", CommandErrorKind.Conflict)));
        Assert.Equal(CommandErrorKind.Unavailable, grammar.Dispatch(new("list", "frontiers", [])).Details.Error);
        Assert.Equal(CommandErrorKind.Unavailable, grammar.Dispatch(new("set", "frontiers", ["x"])).Details.Error);
        Assert.Equal(CommandErrorKind.Conflict, grammar.Dispatch(new("clear", "frontiers", [])).Details.Error);
    }
}
