using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Tests;

public sealed class EntityCatalogInvariantTests
{
    [Fact]
    public void CatalogResolvesAliasesAndRejectsAmbiguity()
    {
        string[] sharedAlias = ["alias"];
        EntityCatalog catalog = new();
        catalog.Replace("frontiers", new[] { new CatalogEntity("one", "First", sharedAlias), new CatalogEntity("two", "Second", sharedAlias) });
        Assert.True(catalog.TryResolve("frontiers", "one", out var found));
        Assert.Equal("First", found!.Name);
        Assert.False(catalog.TryResolve("frontiers", "alias", out _));
        Assert.Empty(catalog.List("doctrines"));
        Assert.Throws<ArgumentException>(() => catalog.Replace("frontiers", new[] { new CatalogEntity("one", "First", Array.Empty<string>()), new CatalogEntity("ONE", "Second", Array.Empty<string>()) }));
    }
}
