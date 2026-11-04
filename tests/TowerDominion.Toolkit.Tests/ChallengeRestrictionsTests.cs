using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class ChallengeRestrictionsTests
{
    [Fact]
    public void NoRestrictionsForEmptyOrUnrelatedChallenges()
    {
        Assert.Equal("", ChallengeRestrictions.Describe(ChallengeRestrictions.For(Array.Empty<string>())));
        Assert.Equal("", ChallengeRestrictions.Describe(ChallengeRestrictions.For(new[] { LowTechRule.Id })));
    }

    [Fact]
    public void CombinedRemovalFlagsAppearOnceForOverlappingChallenges()
    {
        var restrictions = ChallengeRestrictions.For(new[] { ForcedDeploymentRule.Id, ChainOfCommandRule.Id, ForcedDeploymentRule.Id });
        string description = ChallengeRestrictions.Describe(restrictions);
        Assert.True(restrictions.NoSelling);
        Assert.True(restrictions.NoCancelling);
        Assert.Equal(1, description.Split("No Selling").Length - 1);
        Assert.Equal(1, description.Split("No Cancelling").Length - 1);
    }
    [Fact]
    public void ResolvesChallengeBansByCatalogWithoutDuplicatingSharedTargets()
    {
        var catalog = new TowerDominion.Toolkit.Model.EntityCatalog();
        catalog.Replace("blueprints", new[]
        {
            new TowerDominion.Toolkit.Model.CatalogEntity("native-mission", "Mission control", Array.Empty<string>()),
            new TowerDominion.Toolkit.Model.CatalogEntity("native-scout", "Scout Hideout", Array.Empty<string>())
        });
        var both = ChallengeRestrictions.For(new[] { ForcedDeploymentRule.Id, CheapestTowerOnlyRule.Id }, catalog);
        Assert.Equal(2, both.Bans["blueprints"].Count);
        Assert.True(both.IsBanned("blueprints", "native-mission"));
        Assert.True(both.IsBanned("blueprints", "native-scout"));
        Assert.Single(ChallengeRestrictions.For(new[] { ForcedDeploymentRule.Id }, catalog).Bans["blueprints"]);
        Assert.Empty(ChallengeRestrictions.For(new[] { LowTechRule.Id }, catalog).Bans);
    }
    [Fact]
    public void UnresolvedNativeNamesAreNotMistakenForBlueprintIds()
    {
        var catalog = new TowerDominion.Toolkit.Model.EntityCatalog();
        catalog.Replace("blueprints", new[]
        {
            new TowerDominion.Toolkit.Model.CatalogEntity("unrelated-id", "Unrelated Building", Array.Empty<string>())
        });
        var restrictions = ChallengeRestrictions.For(new[] { ForcedDeploymentRule.Id }, catalog);
        Assert.False(restrictions.IsBanned("blueprints", "Mission control"));
        Assert.Empty(restrictions.Bans);
    }
    [Fact]
    public void PopupDisplaysLocalizedNamesNotNativeIds()
    {
        var catalog = new TowerDominion.Toolkit.Model.EntityCatalog();
        catalog.Replace("blueprints", new[]
        {
            new TowerDominion.Toolkit.Model.CatalogEntity("internal-001", "Mission control", Array.Empty<string>()),
            new TowerDominion.Toolkit.Model.CatalogEntity("internal-002", "Scout Hideout", Array.Empty<string>())
        });
        var registry = new TowerDominion.Toolkit.Model.ChallengeRegistry();
        registry.Register(new ForcedDeploymentRule());
        registry.Register(new CheapestTowerOnlyRule());
        string description = ChallengeDisplay.Describe(registry, new[] { ForcedDeploymentRule.Id, CheapestTowerOnlyRule.Id }, catalog);
        Assert.Contains("Mission control", description);
        Assert.Contains("Scout Hideout", description);
        Assert.DoesNotContain("internal-001", description);
        Assert.Equal(1, description.Split("Mission control").Length - 1);
    }
}
