using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Tests;

public sealed class RuntimeCommandRegistrationTests
{
    [Fact]
    public void SeedCommandsUseExistingServiceAndReportState()
    {
        var seeds = new SeedService();
        var grammar = new CommandGrammar();
        RuntimeCommandRegistration.Register(grammar, seeds, () => false, _ => { }, () => "ready");
        var parser = new CommandTextParser(grammar);
        Assert.Equal(6, grammar.Operations.Count);
        var set = parser.Execute("set seed sample-123");
        Assert.True(set.Success);
        Assert.Single(set.Details.Changes!);
        Assert.Null(set.Details.Changes![0].Before);
        Assert.Equal("sample-123", set.Details.Changes[0].After);
        Assert.Empty(parser.Execute("set seed sample-123").Details.Changes!);
        Assert.Equal("sample-123", seeds.PendingOverride);
        var seedStatus = parser.Execute("status seed");
        Assert.Contains("sample-123", seedStatus.Message);
        Assert.Equal("sample-123", seedStatus.Details.Data!["pendingOverride"]);
        Assert.IsType<bool>(seedStatus.Details.Data["verified"]);
        var cleared = parser.Execute("clear seed");
        Assert.True(cleared.Success);
        Assert.Single(cleared.Details.Changes!);
        Assert.Equal("sample-123", cleared.Details.Changes![0].Before);
        Assert.Null(cleared.Details.Changes[0].After);
        Assert.Empty(parser.Execute("clear seed").Details.Changes!);
        Assert.Null(seeds.PendingOverride);
        Assert.Contains("none", parser.Execute("status seed").Message);
        Assert.False(parser.Execute("set seed \"\"").Success);
    }

    [Fact]
    public void DiagnosticsAndToolkitStatusUseLiveDelegates()
    {
        bool enabled = false;
        string status = "initializing";
        var grammar = new CommandGrammar();
        RuntimeCommandRegistration.Register(grammar, new SeedService(), () => enabled,
            value => enabled = value, () => status);
        var parser = new CommandTextParser(grammar);
        Assert.Contains("off", parser.Execute("status frontier diagnostics").Message);
        var enabledResult = parser.Execute("set frontier diagnostics on");
        Assert.True(enabledResult.Success);
        Assert.Single(enabledResult.Details.Changes!);
        Assert.Equal(false, enabledResult.Details.Changes![0].Before);
        Assert.Equal(true, enabledResult.Details.Changes[0].After);
        Assert.Empty(parser.Execute("set frontier diagnostics on").Details.Changes!);
        Assert.True(enabled);
        var diagnosticStatus = parser.Execute("status frontier diagnostics");
        Assert.Contains("on", diagnosticStatus.Message);
        Assert.Equal(true, diagnosticStatus.Details.Data!["enabled"]);
        Assert.False(parser.Execute("set frontier diagnostics invalid").Success);
        Assert.True(enabled);
        Assert.True(parser.Execute("set frontier diagnostics off").Success);
        Assert.False(enabled);
        Assert.Equal("initializing", parser.Execute("status toolkit").Message);
        status = "ready";
        var toolkitStatus = parser.Execute("status toolkit");
        Assert.Equal("ready", toolkitStatus.Message);
        Assert.Equal("ready", toolkitStatus.Details.Data!["status"]);
    }
}
