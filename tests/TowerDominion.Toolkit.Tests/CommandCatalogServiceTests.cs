using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandCatalogServiceTests
{
    private sealed class FakeSource : ICommandCatalogSource
    {
        public bool IsReady { get; set; }
        public List<CatalogEntity> Entities { get; } = [];
        public IReadOnlyList<CatalogEntity> List(string domain) => domain == "frontiers" ? Entities : [];
    }

    [Fact]
    public void UnavailableCatalogIsDistinctFromEmptyCatalog()
    {
        var source = new FakeSource();
        var service = new CommandCatalogService(source);
        Assert.Null(service.List("frontiers"));
        Assert.Null(service.CompletionNames("frontiers"));
        Assert.Equal(CatalogLookupStatus.Unavailable, service.Resolve("frontiers", "Alpha").Status);
        source.IsReady = true;
        Assert.Empty(service.List("frontiers")!);
        Assert.Equal(CatalogLookupStatus.Unknown, service.Resolve("frontiers", "Alpha").Status);
    }

    [Fact]
    public void ExactIdNameAndAliasResolveWithoutGame()
    {
        var source = new FakeSource { IsReady = true };
        source.Entities.Add(new("id-1", "Beta Prime", ["beta"]));
        var service = new CommandCatalogService(source);
        foreach (string value in new[] { "ID-1", "beta prime", "BETA" })
        {
            var result = service.Resolve("frontiers", value);
            Assert.Equal(CatalogLookupStatus.Available, result.Status);
            Assert.Equal("id-1", result.Entity!.Id);
        }
    }

    [Fact]
    public void AmbiguousAliasDoesNotSelectArbitrarily()
    {
        var source = new FakeSource { IsReady = true };
        source.Entities.Add(new("one", "First", ["shared"]));
        source.Entities.Add(new("two", "Second", ["shared"]));
        var result = new CommandCatalogService(source).Resolve("frontiers", "shared");
        Assert.Equal(CatalogLookupStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.Matches!.Count);
    }

    [Fact]
    public void CompletionReadsFreshSortedCatalogSnapshot()
    {
        var source = new FakeSource { IsReady = true };
        source.Entities.Add(new("z", "Zulu", []));
        var service = new CommandCatalogService(source);
        Assert.Equal(["Zulu"], service.CompletionNames("frontiers"));
        source.Entities.Add(new("a", "Alpha", []));
        Assert.Equal(["Alpha", "Zulu"], service.CompletionNames("frontiers"));
    }
}
