using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Tests;

public sealed class BuildInfoTests
{
    [Fact]
    public void VersionAndCommitAreEmbedded()
    {
        Assert.Equal("Tower Dominion Toolkit", ToolkitBuildInfo.Name);
        Assert.Equal("0.1.1", ToolkitBuildInfo.Version);
        Assert.False(string.IsNullOrWhiteSpace(ToolkitBuildInfo.Commit));
    }
}
