using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ReservedAllTests
{
    [Theory]
    [InlineData("all,one", false)]
    [InlineData("one,all", false)]
    [InlineData("\"all\",one", true)]
    [InlineData("one,\"all\"", true)]
    [InlineData("\"all\"", true)]
    [InlineData("all", true)]
    public void ReservedAllMustStandAloneUnlessQuoted(string selection, bool expected)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members", CommaSeparated: true)],
            Handler: _ => CommandOutcome.Ok("ok")));
        Assert.Equal(expected, new CommandTextParser(grammar).Execute("select frontiers " + selection).Success);
    }
}
