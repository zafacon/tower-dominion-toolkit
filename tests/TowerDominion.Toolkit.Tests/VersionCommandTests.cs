using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class VersionCommandTests
{
    [Fact]
    public void ReportsBuildIdentityAndCurrentRegistrations()
    {
        CommandRegistry commands = new();
        ModuleRegistry modules = new();
        CapabilityRegistry capabilities = new();
        VersionCommands version = new(commands, modules, capabilities);
        commands.Register(new("version", "version", "Show version.", version.Execute));
        commands.Register(new("seed", "seed status", "Seed management.", _ => "ok"));
        modules.Register(new SeedModule(new SeedService()));
        capabilities.Set(new CapabilityDescriptor("seed-control", CapabilityAvailability.Available));

        string output = commands.Execute("VERSION");
        Assert.Contains($"Tower Dominion Toolkit v{ToolkitBuildInfo.Version}", output);
        Assert.Contains($"Build commit: {ToolkitBuildInfo.Commit}", output);
        Assert.Contains("Commands: seed, version", output);
        Assert.Contains("seed-control", output);
        Assert.Contains("Modules:", output);
        Assert.Equal("Usage: version", commands.Execute("version unexpected"));
    }

    [Fact]
    public void ReflectsChangesToRegistryWithoutStaticFeatureList()
    {
        CommandRegistry commands = new();
        VersionCommands version = new(commands, new ModuleRegistry(), new CapabilityRegistry());
        commands.Register(new("version", "version", "Version.", version.Execute));
        Assert.DoesNotContain("new-feature", commands.Execute("version"));
        commands.Register(new("new-feature", "new-feature", "New.", _ => "ok"));
        Assert.Contains("new-feature", commands.Execute("version"));
    }
}
