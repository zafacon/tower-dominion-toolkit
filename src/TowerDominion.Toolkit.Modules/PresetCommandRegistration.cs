namespace TowerDominion.Toolkit.Modules;

/// <summary>Connects domain snapshot adapters to canonical preset grammar operations.</summary>
public static class PresetCommandRegistration
{
    public static void Register(CommandGrammar grammar, SelectionPresetAdapters selections,
        ChallengePresetAdapter challenges, StartingTilePresetAdapter startingTiles)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentNullException.ThrowIfNull(challenges);
        ArgumentNullException.ThrowIfNull(startingTiles);
        RegisterDomain(grammar, "bans", name => selections.Save("bans", name),
            name => selections.Load("bans", name), () => selections.Names("bans"),
            name => selections.Inspect("bans", name), name => selections.Delete("bans", name),
            () => selections.SnapshotBans().ToDictionary(x => x.Key, x => x.Value));
        RegisterDomain(grammar, "frontiers", name => selections.Save("frontiers", name),
            name => selections.Load("frontiers", name), () => selections.Names("frontiers"),
            name => selections.Inspect("frontiers", name), name => selections.Delete("frontiers", name),
            () => new Dictionary<string, string[]> { ["selected"] = selections.SnapshotFrontiers() });
        RegisterDomain(grammar, "challenges", challenges.Save, challenges.Load, challenges.Names,
            name => string.Join(", ", challenges.Inspect(name)), challenges.Delete,
            () => new Dictionary<string, string[]> { ["selected"] = challenges.Snapshot() });
        RegisterDomain(grammar, "starting tiles", startingTiles.Save,
            name => { startingTiles.Load(name); }, startingTiles.Names,
            startingTiles.Inspect, startingTiles.Delete,
            () => new Dictionary<string, string[]> { ["configuration"] = startingTiles.Snapshot() },
            startingTiles.PreviewCurrent);
    }

    private static void RegisterDomain(CommandGrammar grammar, string domain,
        Action<string> save, Action<string> load, Func<IReadOnlyList<string>> names,
        Func<string, string> inspect, Func<string, bool> delete,
        Func<IReadOnlyDictionary<string, string[]>> snapshot, Func<CommandPreview>? preview = null)
    {
        if (!grammar.Domains.Any(x => x.Name.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            grammar.RegisterDomain(new(domain, $"{domain} configuration"));
        grammar.RegisterPresets(domain, new(
            invocation => Execute(() => { save(Name(invocation)); return $"Saved {domain} preset '{Name(invocation)}'."; }),
            invocation => ExecuteLoad(domain, Name(invocation), load, snapshot, preview),
            _ => Execute(() => $"{domain} presets: {string.Join(", ", names())}"),
            invocation => Execute(() => $"{domain} preset '{Name(invocation)}': {inspect(Name(invocation))}"),
            invocation => Execute(() => delete(Name(invocation))
                ? $"Deleted {domain} preset '{Name(invocation)}'."
                : $"No {domain} preset named '{Name(invocation)}'.")), names);
    }

    private static CommandOutcome ExecuteLoad(string domain, string name, Action<string> load,
        Func<IReadOnlyDictionary<string, string[]>> snapshot, Func<CommandPreview>? preview)
    {
        return Execute(() =>
        {
            var before = snapshot();
            load(name);
            var after = snapshot();
            var changes = before.Keys.Union(after.Keys, StringComparer.OrdinalIgnoreCase)
                .Select(key => new CommandStateChange(domain == "bans" ? key : domain,
                    domain == "bans" ? "banned" : domain == "starting tiles" ? "configuration" : "selected",
                    before.TryGetValue(key, out var previous) ? previous : Array.Empty<string>(),
                    after.TryGetValue(key, out var current) ? current : Array.Empty<string>()))
                .Where(change => !((string[])change.Before!).SequenceEqual((string[])change.After!,
                    StringComparer.OrdinalIgnoreCase))
                .ToArray();
            return CommandOutcome.Ok($"Loaded {domain} preset '{name}'.",
                new CommandResultDetails(Data: new Dictionary<string, object?>
                {
                    ["preset"] = name,
                    ["changedDomains"] = changes.Length
                }, Changes: changes, Preview: preview?.Invoke()));
        });
    }

    private static string Name(CommandInvocation invocation) => invocation.Arguments[0];

    private static CommandOutcome Execute(Func<CommandOutcome> action)
    {
        try { return action(); }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
        {
            return CommandOutcome.Failure(ex.Message, ex switch
            {
                ArgumentException => CommandErrorKind.Validation,
                InvalidOperationException => CommandErrorKind.Conflict,
                _ => CommandErrorKind.Persistence
            });
        }
    }

    private static CommandOutcome Execute(Func<string> action)
    {
        try { return CommandOutcome.Ok(action()); }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
        {
            return CommandOutcome.Failure(ex.Message, ex switch
            {
                ArgumentException => CommandErrorKind.Validation,
                InvalidOperationException => CommandErrorKind.Conflict,
                _ => CommandErrorKind.Persistence
            });
        }
    }
}
