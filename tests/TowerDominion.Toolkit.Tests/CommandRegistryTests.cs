using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class CommandRegistryTests
{
    [Fact]
    public void RoutesSeedCommandsAndProvidesHelp()
    {
        SeedService seeds = new();
        CommandRegistry registry = new();
        registry.Register(new("seed", "seed set <value> | seed clear | seed status", "Control the seed.", new SeedCommands(new SeedModule(seeds), seeds).Execute));
        Assert.Contains("seed set", registry.Execute("help"));
        Assert.Contains("Control the seed", registry.Execute("help seed"));
        Assert.Contains("Accepted", registry.Execute("SEED set 42"));
        Assert.Equal("42", seeds.PendingOverride);
        Assert.Contains("Unknown command", registry.Execute("unknown"));
        Assert.Throws<InvalidOperationException>(() => registry.Register(new("SEED", "seed", "Duplicate", _ => "")));
    }

    private static readonly string[] sharedAlias = ["alias"];

    [Fact]
    public void CatalogResolvesAliasesAndRejectsAmbiguity()
    {
        EntityCatalog catalog = new();
        catalog.Replace("frontiers", new[] { new CatalogEntity("one", "First", sharedAlias), new CatalogEntity("two", "Second", sharedAlias) });
        Assert.True(catalog.TryResolve("frontiers", "one", out var found));
        Assert.Equal("First", found!.Name);
        Assert.False(catalog.TryResolve("frontiers", "alias", out _));
        Assert.Empty(catalog.List("doctrines"));
        Assert.Throws<ArgumentException>(() => catalog.Replace("frontiers", new[] { new CatalogEntity("one", "First", Array.Empty<string>()), new CatalogEntity("ONE", "Second", Array.Empty<string>()) }));
    }
}
