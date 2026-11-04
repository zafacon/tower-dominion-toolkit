using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandValueCompletionTests
{
    [Theory]
    [InlineData("Alpha, Be", 9, "Beta Prime", "Alpha, \"Beta Prime\"")]
    [InlineData("\"Be\"", 3, "Beta Prime", "\"Beta Prime\"")]
    [InlineData("Alpha, \"Be\"", 11, "Beta Prime", "Alpha, \"Beta Prime\"")]
    [InlineData("Al", 2, "Alpha", "Alpha")]
    public void CompletesQuotedOrCommaMembersWithoutChangingEarlierMembers(string input, int cursor, string candidate, string expected)
    {
        var result = CommandValueCompletion.Complete(input, cursor, [candidate], true);
        Assert.Equal(expected, result.Text);
    }

    [Theory]
    [InlineData("\"\"", 0)]
    [InlineData("\"Alpha\"", 0)]
    [InlineData("Alpha, \"Beta\"", 7)]
    public void CursorBeforeOpeningQuoteDoesNotThrow(string input, int cursor)
    {
        var exception = Record.Exception(() => CommandValueCompletion.Complete(input, cursor, ["Alpha", "Beta"], true));
        Assert.Null(exception);
    }

    [Fact]
    public void NoMatchesPreserveInput()
    {
        var result = CommandValueCompletion.Complete("Alpha, Unknown", 14, ["Beta"], true);
        Assert.False(result.Changed);
        Assert.Equal("Alpha, Unknown", result.Text);
    }
}
