using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CatalogSelectionServiceTests
{
    private sealed class FakeSource : ICommandCatalogSource
    {
        public bool IsReady { get; set; } = true;
        public IReadOnlyList<CatalogEntity> List(string domain) => domain switch
        {
            "frontiers" or "foes" => [new("id-a", "Alpha", ["first"]), new("id-b", "Beta Prime", ["second"])],
            _ => []
        };
    }

    private static (CatalogSelectionHandlers Handlers, BanSelections Bans, FrontierSelections Frontiers, FakeSource Source) Create()
    {
        var source = new FakeSource();
        var resolver = new CatalogSelectionService(new CommandCatalogService(source));
        var bans = new BanSelections("unused.json", persist: false);
        var frontiers = new FrontierSelections(Path.Combine(Path.GetTempPath(), "td-frontier-" + Guid.NewGuid() + ".json"));
        return (new(resolver, bans, frontiers), bans, frontiers, source);
    }

    [Fact]
    public void UnknownSecondMemberDoesNotPartiallyBan()
    {
        var (handlers, bans, _, _) = Create();
        var result = handlers.Ban("foes", ["Alpha", "missing"], false, true);
        Assert.False(result.Success);
        Assert.Equal(CommandErrorKind.Validation, result.Details.Error);
        Assert.Null(result.Details.Changes);
        Assert.Empty(bans.List("foes"));
    }

    [Fact]
    public void ValidNamesAliasesAndIdsAreDeduplicatedBeforeMutation()
    {
        var (handlers, bans, _, _) = Create();
        Assert.True(handlers.Ban("foes", ["Alpha", "first", "id-b"], false, true).Success);
        Assert.Equal(2, bans.List("foes").Count);
        Assert.True(bans.IsBanned("foes", "id-a"));
        Assert.True(bans.IsBanned("foes", "id-b"));
    }

    [Fact]
    public void AllUsesCatalogIdsAndUnbanReversesThem()
    {
        var (handlers, bans, _, _) = Create();
        Assert.True(handlers.Ban("foes", [], true, true).Success);
        Assert.Equal(2, bans.List("foes").Count);
        Assert.True(handlers.Ban("foes", [], true, false).Success);
        Assert.Empty(bans.List("foes"));
    }

    [Fact]
    public void UnavailableCatalogDoesNotChangeFrontiers()
    {
        var (handlers, _, frontiers, source) = Create();
        source.IsReady = false;
        var result = handlers.Frontier(["Alpha"], false, true);
        Assert.False(result.Success);
        Assert.Equal(CommandErrorKind.Unavailable, result.Details.Error);
        Assert.Null(result.Details.Changes);
        Assert.Empty(frontiers.SelectedIds);
    }

    [Fact]
    public void FrontierSelectionUsesSameResolutionAndPersists()
    {
        var (handlers, _, frontiers, _) = Create();
        Assert.True(handlers.Frontier(["second"], false, true).Success);
        Assert.Contains("id-b", frontiers.SelectedIds);
        Assert.True(handlers.Frontier(["Beta Prime"], false, false).Success);
        Assert.Empty(frontiers.SelectedIds);
    }

    [Fact]
    public void MixedAllAndNamesFailsBeforeMutation()
    {
        var (handlers, bans, _, _) = Create();
        var result = handlers.Ban("foes", ["Alpha"], true, true);
        Assert.False(result.Success);
        Assert.Equal(CommandErrorKind.Validation, result.Details.Error);
        Assert.Null(result.Details.Changes);
        Assert.Empty(bans.List("foes"));
    }
}
