using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ArgumentNameUniquenessTests
{
    [Theory]
    [InlineData("members", "members")]
    [InlineData("members", "MEMBERS")]
    public void RegistrationRejectsDuplicateArgumentNames(string first, string second)
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        Assert.Throws<ArgumentException>(() => grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition(first, "First", CommaSeparated: true),
             new ArgumentDefinition(second, "Second", CommaSeparated: true)])));
        Assert.Empty(grammar.Operations);
    }

    [Fact]
    public void DistinctArgumentNamesAreAccepted()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("frontiers", "Modifiers"));
        grammar.Register(new("select", "frontiers", "Select",
            [new ArgumentDefinition("members", "Members"),
             new ArgumentDefinition("mode", "Mode")]));
        Assert.Single(grammar.Operations);
    }
}
