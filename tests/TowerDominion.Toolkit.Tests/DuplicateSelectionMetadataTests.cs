using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class DuplicateSelectionMetadataTests
{
    [Fact]
    public void CaseVariantDuplicateAllFlagsAreRejected()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        var result = grammar.Dispatch(new("select", "frontiers", ["all"], AllSelections: ["members", "MEMBERS"]));
        Assert.False(result.Success);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void CaseVariantDuplicateMemberKeysAreRejected()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        var members = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["members"] = ["one"],
            ["MEMBERS"] = ["one"]
        };
        var result = grammar.Dispatch(new("select", "frontiers", ["one"], SelectionMembers: members));
        Assert.False(result.Success);
        Assert.Equal(0, calls);
    }

    private static CommandGrammar Create(Action invoked)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true)],
            Handler: _ => { invoked(); return CommandOutcome.Ok("ok"); }));
        return grammar;
    }
}
