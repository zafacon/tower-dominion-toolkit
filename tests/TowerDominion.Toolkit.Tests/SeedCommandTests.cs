using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class SeedCommandTests
{
    [Fact]
    public void SetStatusAndClearAreHandledWithoutGameDependencies()
    {
        SeedService service = new();
        SeedCommands commands = new(new SeedModule(service), service);

        Assert.Contains("Accepted", commands.Execute("seed set 123"));
        Assert.Contains("Pending: 123", commands.Execute("seed status"));
        Assert.Equal("Pending seed override cleared.", commands.Execute("seed clear"));
        Assert.Contains("Pending: none", commands.Execute("seed status"));
    }

    [Fact]
    public void InvalidCommandsDoNotMutateSeed()
    {
        SeedService service = new();
        SeedCommands commands = new(new SeedModule(service), service);

        Assert.Contains("Usage:", commands.Execute("seed nope"));
        Assert.Null(service.PendingOverride);
    }
}
