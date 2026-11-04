using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Headless runtime controls; game-owned diagnostic state is accessed through delegates.</summary>
public static class RuntimeCommandRegistration
{
    private static readonly string[] DiagnosticOptions = { "on", "off" };

    public static void Register(CommandGrammar grammar, ISeedService seeds,
        Func<bool> diagnosticsEnabled, Action<bool> setDiagnostics, Func<string> toolkitStatus)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(seeds);
        ArgumentNullException.ThrowIfNull(diagnosticsEnabled);
        ArgumentNullException.ThrowIfNull(setDiagnostics);
        ArgumentNullException.ThrowIfNull(toolkitStatus);
        foreach (string domain in new[] { "seed", "frontier diagnostics", "toolkit" })
            if (!grammar.Domains.Any(d => d.Name.Equals(domain, StringComparison.OrdinalIgnoreCase)))
                grammar.RegisterDomain(new(domain, domain));
        grammar.Register(new("set", "seed", "Request a seed override",
            new[] { new ArgumentDefinition("value", "Seed value", Completion: ArgumentCompletion.FreeForm) },
            Handler: invocation =>
            {
                string? before = seeds.PendingOverride;
                var result = seeds.RequestOverride(invocation.Arguments[0]).Result;
                if (result.Status is OperationStatus.Rejected or OperationStatus.Failed)
                    return CommandOutcome.Failure(result.Reason ?? "Seed override rejected.",
                        result.Status == OperationStatus.Rejected ? CommandErrorKind.Validation : CommandErrorKind.Execution);
                string? after = seeds.PendingOverride;
                var changes = string.Equals(before, after, StringComparison.Ordinal)
                    ? Array.Empty<CommandStateChange>()
                    : new[] { new CommandStateChange("seed", "pendingOverride", before, after) };
                return CommandOutcome.Ok($"Seed override {result.Status}: {invocation.Arguments[0]}",
                    new CommandResultDetails(Data: new Dictionary<string, object?>
                    {
                        ["status"] = result.Status.ToString(),
                        ["pendingOverride"] = after
                    }, Changes: changes));
            }));
        grammar.Register(new("clear", "seed", "Clear pending seed override", Array.Empty<ArgumentDefinition>(),
            Handler: _ => ClearSeed(seeds)));
        grammar.Register(new("status", "seed", "Seed override and verification status", Array.Empty<ArgumentDefinition>(),
            Handler: _ =>
            {
                var snapshot = seeds.Seed.Snapshot;
                return CommandOutcome.Ok($"Pending: {seeds.PendingOverride ?? "none"}; Observed: {snapshot.Observed ?? "unknown"}; Applied: {snapshot.Applied ?? "none"}; Verified: {snapshot.IsVerified}",
                    new CommandResultDetails(Data: new Dictionary<string, object?>
                    {
                        ["pendingOverride"] = seeds.PendingOverride,
                        ["observed"] = snapshot.Observed,
                        ["applied"] = snapshot.Applied,
                        ["verified"] = snapshot.IsVerified
                    }));
            }));
        grammar.Register(new("set", "frontier diagnostics", "Enable or disable Frontier tracing",
            new[] { new ArgumentDefinition("state", "on or off", Completion: ArgumentCompletion.Literal,
                AllowedValues: DiagnosticOptions) },
            Handler: invocation =>
            {
                bool enabled = invocation.Arguments[0].Equals("on", StringComparison.OrdinalIgnoreCase);
                bool before = diagnosticsEnabled();
                setDiagnostics(enabled);
                bool after = diagnosticsEnabled();
                return CommandOutcome.Ok($"Frontier diagnostics: {(enabled ? "on" : "off")}.",
                    new CommandResultDetails(Data: new Dictionary<string, object?> { ["enabled"] = after },
                        Changes: before == after ? Array.Empty<CommandStateChange>() :
                            new[] { new CommandStateChange("frontier diagnostics", "enabled", before, after) }));
            }));
        grammar.Register(new("status", "frontier diagnostics", "Frontier tracing status", Array.Empty<ArgumentDefinition>(),
            Handler: _ => CommandOutcome.Ok($"Frontier diagnostics: {(diagnosticsEnabled() ? "on" : "off")}.",
                new CommandResultDetails(Data: new Dictionary<string, object?> { ["enabled"] = diagnosticsEnabled() }))));
        grammar.Register(new("status", "toolkit", "Toolkit status", Array.Empty<ArgumentDefinition>(),
            Handler: _ =>
            {
                string status = toolkitStatus();
                return CommandOutcome.Ok(status,
                    new CommandResultDetails(Data: new Dictionary<string, object?> { ["status"] = status }));
            }));
    }

    private static CommandOutcome ClearSeed(ISeedService seeds)
    {
        string? before = seeds.PendingOverride;
        bool cleared = seeds.ClearOverride();
        string? after = seeds.PendingOverride;
        return CommandOutcome.Ok(cleared ? "Pending seed override cleared." : "No pending seed override.",
            new CommandResultDetails(Data: new Dictionary<string, object?> { ["cleared"] = cleared },
                Changes: string.Equals(before, after, StringComparison.Ordinal)
                    ? Array.Empty<CommandStateChange>()
                    : new[] { new CommandStateChange("seed", "pendingOverride", before, after) }));
    }
}
