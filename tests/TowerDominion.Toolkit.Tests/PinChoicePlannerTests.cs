using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class PinChoicePlannerTests
{
    private sealed record Choice(string Id, bool Mandatory);

    [Fact]
    public void NeverDisplacesMandatoryOrAlreadyPinnedChoices()
    {
        Choice[] selected = [new("mandatory", true), new("pinned", false), new("ordinary", false)];
        Assert.Equal(2, PinChoicePlanner.FindReplacement(selected, ["pinned"], x => x.Id, x => x.Mandatory));
        Assert.Equal(-1, PinChoicePlanner.FindReplacement(selected[..2], ["pinned"], x => x.Id, x => x.Mandatory));
    }

    [Fact]
    public void MatchesProtectedIdentifiersCaseInsensitively()
    {
        Choice[] selected = [new("ALPHA", false), new("BETA", false)];
        Assert.Equal(1, PinChoicePlanner.FindReplacement(selected, ["alpha"], x => x.Id, x => x.Mandatory));
        Assert.Equal(-1, PinChoicePlanner.FindReplacement(selected, ["beta", "alpha"], x => x.Id, x => x.Mandatory));
    }

    [Fact]
    public void PreservesMandatoryChoicesWhenEverySlotIsMandatory()
    {
        Choice[] selected = [new("first", true), new("second", true)];
        Assert.Equal(-1, PinChoicePlanner.FindReplacement(selected, Array.Empty<string>(), x => x.Id, x => x.Mandatory));
    }

    [Fact]
    public void AllowsReplacingAnUnpinnedNonmandatoryChoice()
    {
        Choice[] selected = [new("mandatory", true), new("ordinary", false), new("already-pinned", false)];
        Assert.Equal(1, PinChoicePlanner.FindReplacement(selected, ["already-pinned"], x => x.Id, x => x.Mandatory));
    }
}
