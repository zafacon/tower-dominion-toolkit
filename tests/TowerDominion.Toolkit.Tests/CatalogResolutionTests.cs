using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CatalogResolutionTests
{
    [Theory]
    [InlineData("one,two", true)]
    [InlineData("ONE,Two", true)]
    [InlineData("one,missing", false)]
    [InlineData("all", true)]
    public void ResolvesCatalogSelectionsBeforeHandler(string selection, bool valid)
    {
        IReadOnlyList<string>? available = ["one", "two"];
        int calls = 0;
        var grammar = Create(() => available, () => calls++);
        Assert.Equal(valid, new CommandTextParser(grammar).Execute("select frontiers " + selection).Success);
        Assert.Equal(valid ? 1 : 0, calls);
    }

    [Fact]
    public void UnavailableCatalogDiffersFromUnknownName()
    {
        IReadOnlyList<string>? available = null;
        int calls = 0;
        var grammar = Create(() => available, () => calls++);
        var parser = new CommandTextParser(grammar);
        Assert.Contains("Catalog unavailable", parser.Execute("select frontiers one").Message);
        available = ["one"];
        Assert.Contains("Unknown", parser.Execute("select frontiers missing").Message);
        Assert.True(parser.Execute("select frontiers one").Success);
        Assert.Equal(1, calls);
    }

    private static CommandGrammar Create(Func<IReadOnlyList<string>?> provider, Action invoked)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", Completion: ArgumentCompletion.Catalog,
                CommaSeparated: true, CatalogProvider: provider)],
            Handler: _ => { invoked(); return CommandOutcome.Ok("ok"); }));
        return grammar;
    }
}
