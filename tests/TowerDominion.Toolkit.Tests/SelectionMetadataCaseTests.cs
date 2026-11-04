using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class SelectionMetadataCaseTests
{
    [Fact]
    public void CaseInsensitiveMetadataKeyResolvesCatalog()
    {
        var grammar = Create();
        var result = grammar.Dispatch(new("select", "frontiers", ["one"],
            SelectionMembers: new Dictionary<string, IReadOnlyList<string>> { ["MEMBERS"] = ["one"] }));
        Assert.True(result.Success);
    }

    [Fact]
    public void EmptyMetadataDoesNotAuthorizeQuotedSelection()
    {
        var grammar = Create();
        var result = grammar.Dispatch(new("select", "frontiers", ["\"one\""],
            SelectionMembers: new Dictionary<string, IReadOnlyList<string>>()));
        Assert.False(result.Success);
    }

    private static CommandGrammar Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true,
                CatalogProvider: () => ["one"])],
            Handler: _ => CommandOutcome.Ok("ok")));
        return grammar;
    }
}
