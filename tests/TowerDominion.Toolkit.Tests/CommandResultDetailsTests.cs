using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandResultDetailsTests
{
    [Fact]
    public void LegacyOutcomeFactoriesRemainCompatible()
    {
        var success = CommandOutcome.Ok("done");
        var failure = CommandOutcome.Failure("bad");
        Assert.True(success.Success);
        Assert.False(failure.Success);
        Assert.Equal("done", success.Message);
        Assert.Equal("bad", failure.Message);
        Assert.Equal(CommandErrorKind.None, success.Details.Error);
        Assert.Equal(CommandErrorKind.None, failure.Details.Error);
        Assert.Null(success.Details.Data);
        Assert.Null(success.Details.Changes);
        Assert.Null(success.Details.Preview);
    }

    [Fact]
    public void StructuredResultCarriesDataChangesAndPreviewWithoutFormattingDependency()
    {
        var data = new Dictionary<string, object?> { ["selected"] = new[] { "alpha" } };
        string[] selected = ["alpha"];
        var changes = new[] { new CommandStateChange("frontiers", "selected", Array.Empty<string>(), selected) };
        var preview = new CommandPreview("selection", new Dictionary<string, object?> { ["count"] = 1 });
        var result = CommandOutcome.Ok("Selected alpha", new CommandResultDetails(
            Data: data, Changes: changes, Preview: preview));
        Assert.True(result.Success);
        Assert.Same(data, result.Details.Data);
        Assert.Same(changes, result.Details.Changes);
        Assert.Same(preview, result.Details.Preview);
        Assert.Equal("selection", result.Details.Preview!.Kind);
        Assert.Equal("selected", result.Details.Changes![0].Property);
    }

    [Fact]
    public void TypedFailurePreservesMessageAndErrorCategory()
    {
        var result = CommandOutcome.Failure("Unknown item", CommandErrorKind.Validation);
        Assert.False(result.Success);
        Assert.Equal("Unknown item", result.Message);
        Assert.Equal(CommandErrorKind.Validation, result.Details.Error);
    }
}
