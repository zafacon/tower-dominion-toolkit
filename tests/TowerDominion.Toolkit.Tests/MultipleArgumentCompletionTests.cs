using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class MultipleArgumentCompletionTests
{
    private static (CommandArgumentCompletion Completion, CommandTextParser Parser) Create()
    {
        var grammar = new CommandGrammar();
        grammar.RegisterDomain(new("diagnostics", "Diagnostics"));
        grammar.Register(new("set", "diagnostics", "Set", [
            new ArgumentDefinition("scope", "Scope", AllowedValues: ["frontier", "wave"]),
            new ArgumentDefinition("state", "State", AllowedValues: ["on", "off"])]));
        return (new(grammar), new(grammar));
    }

    [Theory]
    [InlineData("set diagnostics fr off", 18, "set diagnostics frontier off")]
    [InlineData("set diagnostics frontier of", 27, "set diagnostics frontier off")]
    [InlineData("set diagnostics fr on", 18, "set diagnostics frontier on")]
    public void CompletionOfEitherArgumentPreservesTheOther(string input, int cursor, string expected)
    {
        var (completion, parser) = Create();
        var result = completion.Complete(input, cursor);
        Assert.Equal(expected, result.Completion.Text);
        Assert.NotNull(parser.Parse(result.Completion.Text).Invocation);
    }

    [Fact]
    public void EditingEarlierArgumentKeepsLaterArgumentAndCursor()
    {
        var (completion, _) = Create();
        var result = completion.Complete("set diagnostics fr off", 18);
        Assert.Equal("set diagnostics frontier off", result.Completion.Text);
        Assert.Equal(24, result.Completion.Cursor);
    }
}
