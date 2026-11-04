using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ReservedAllDispatchParityTests
{
    [Fact]
    public void DirectDispatchCannotReinterpretBareAllAsLiteralMember()
    {
        int calls = 0;
        var grammar = Create(() => calls++);
        var result = grammar.Dispatch(new("select", "frontiers", ["all"],
            SelectionMembers: new Dictionary<string, IReadOnlyList<string>> { ["members"] = ["all"] }));
        Assert.False(result.Success);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void QuotedAllRemainsLiteralAndBareAllRemainsReserved()
    {
        var grammar = Create(() => { });
        var parser = new CommandTextParser(grammar);
        var quoted = parser.Parse("select frontiers \"all\"");
        Assert.NotNull(quoted.Invocation);
        Assert.Empty(quoted.Invocation.AllSelections!);
        Assert.Equal(["all"], quoted.Invocation.SelectionMembers!["members"]);
        Assert.True(grammar.Dispatch(quoted.Invocation).Success);
        var reserved = parser.Parse("select frontiers all");
        Assert.NotNull(reserved.Invocation);
        Assert.Contains("members", reserved.Invocation.AllSelections!);
        Assert.True(grammar.Dispatch(reserved.Invocation).Success);
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
