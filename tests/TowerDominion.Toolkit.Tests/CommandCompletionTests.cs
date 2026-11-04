using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandCompletionTests
{

    [Theory]
    [InlineData("sel", 3, "select", 6)]
    [InlineData("sel frontiers", 3, "select frontiers", 6)]
    [InlineData("seect frontiers", 2, "select frontiers", 6)]
    [InlineData("list fron", 9, "list frontiers", 14)]
    public void UniqueMatchReplacesOnlyCurrentToken(string input, int cursor, string expected, int expectedCursor)
    {
        var result = CommandCompletion.Complete(input, cursor, ["select", "list", "frontiers"]);
        Assert.True(result.Changed);
        Assert.Equal(expected, result.Text);
        Assert.Equal(expectedCursor, result.Cursor);
    }

    [Fact]
    public void MultipleMatchesExpandCommonPrefix()
    {
        var result = CommandCompletion.Complete("s", 1, ["select", "set", "status"]);
        Assert.Equal("s", result.Text);
        Assert.Equal(3, result.Candidates.Count);
        Assert.False(result.Changed);
        var expanded = CommandCompletion.Complete("se", 2, ["select", "set"]);
        Assert.Equal("se", expanded.Text);
        Assert.False(expanded.Changed);
        Assert.Equal("sel", CommandCompletion.Complete("s", 1, ["select", "self"]).Text);
    }

    [Fact]
    public void NoMatchPreservesInputAndCursor()
    {
        var result = CommandCompletion.Complete("unknown frontiers", 3, ["select", "set"]);
        Assert.False(result.Changed);
        Assert.Equal("unknown frontiers", result.Text);
        Assert.Equal(3, result.Cursor);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void MiddleOfTokenMustMatchBothSides()
    {
        var result = CommandCompletion.Complete("seect frontiers", 2, ["select", "set", "send"]);
        Assert.Single(result.Candidates);
        Assert.Equal("select", result.Candidates[0]);
    }

    [Fact]
    public void InvalidCursorIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandCompletion.Complete("abc", -1, ["abc"]));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandCompletion.Complete("abc", 4, ["abc"]));
    }
}
