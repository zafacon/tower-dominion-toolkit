namespace TowerDominion.Toolkit.Modules;

/// <summary>Canonical queries and difficulty-relative starting-tile mutations.</summary>
public static class StartingTileCommandRegistration
{
    private static readonly string[] Difficulties = { "D1", "D2", "D3", "D4" };
    public static void Register(CommandGrammar grammar, StartingTilePresets tiles)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(tiles);
        if (!grammar.Domains.Any(d => d.Name.Equals("starting tiles", StringComparison.OrdinalIgnoreCase)))
            grammar.RegisterDomain(new("starting tiles", "Starting tile overrides"));
        grammar.Register(new("list", "starting tiles", "List current starting tiles", Array.Empty<ArgumentDefinition>(),
            Handler: _ => DescribeOutcome(tiles)));
        grammar.Register(new("inspect", "starting tiles", "Inspect current starting tiles", Array.Empty<ArgumentDefinition>(),
            Handler: _ => DescribeOutcome(tiles)));
        grammar.Register(new("set", "starting tiles", "Set relative shapes for an inferred difficulty",
            new[] { new ArgumentDefinition("shapes", "One to four comma-separated tile shapes",
                Completion: ArgumentCompletion.FreeForm) },
            Handler: invocation => Execute(tiles, () =>
            {
                string shapes = invocation.Arguments[0];
                if (shapes.Contains(':')) throw new ArgumentException("Use relative tile shapes without compass positions.");
                int difficulty = shapes.Split(',').Length;
                tiles.SetRelative(difficulty, shapes);
                return $"D{difficulty} starting tiles set.";
            })));
        grammar.Register(new("clear", "starting tiles", "Clear all or one difficulty",
            new[] { new ArgumentDefinition("difficulty", "Optional D1, D2, D3 or D4",
                Required: false, Completion: ArgumentCompletion.Literal, AllowedValues: Difficulties) },
            Handler: invocation => Execute(tiles, () =>
            {
                if (invocation.Arguments.Count == 0)
                {
                    foreach (int difficulty in tiles.Relative.Keys.ToArray()) tiles.ClearRelative(difficulty);
                    return "All relative starting tiles cleared.";
                }
                int selected = int.Parse(invocation.Arguments[0].Substring(1), System.Globalization.CultureInfo.InvariantCulture);
                tiles.ClearRelative(selected);
                return $"D{selected} starting tiles cleared.";
            })));
    }

    private static CommandOutcome DescribeOutcome(StartingTilePresets tiles)
    {
        CommandPreview preview = StartingTilePreview.Create(tiles);
        return CommandOutcome.Ok(Describe(tiles), new CommandResultDetails(
            Data: preview.Data, Preview: preview));
    }

    private static string Describe(StartingTilePresets tiles)
    {
        string relative = tiles.Relative.Count == 0 ? "No relative starting tiles." :
            string.Join("; ", tiles.Relative.OrderBy(p => p.Key)
                .Select(p => $"D{p.Key}: {string.Join(",", p.Value)}"));
        return tiles.Active is null ? relative : relative + " | Absolute: " +
            string.Join(", ", tiles.Active.Values.Select(StartingTilePresets.Format));
    }

    private static CommandOutcome Execute(StartingTilePresets tiles, Func<string> action)
    {
        var before = Snapshot(tiles);
        try
        {
            string message = action();
            var after = Snapshot(tiles);
            var changes = before.Keys.Union(after.Keys)
                .OrderBy(key => key)
                .Where(key => !before.GetValueOrDefault(key, Array.Empty<string>())
                    .SequenceEqual(after.GetValueOrDefault(key, Array.Empty<string>()), StringComparer.OrdinalIgnoreCase))
                .Select(key => new CommandStateChange("starting tiles", $"D{key}",
                    before.GetValueOrDefault(key, Array.Empty<string>()),
                    after.GetValueOrDefault(key, Array.Empty<string>())))
                .ToArray();
            return CommandOutcome.Ok(message, new CommandResultDetails(
                Data: new Dictionary<string, object?> { ["changedDifficulties"] = changes.Length },
                Changes: changes, Preview: StartingTilePreview.Create(tiles)));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
        {
            return CommandOutcome.Failure(ex.Message, ex switch
            {
                ArgumentException => CommandErrorKind.Validation,
                InvalidOperationException => CommandErrorKind.Conflict,
                _ => CommandErrorKind.Persistence
            });
        }
    }

    private static Dictionary<int, string[]> Snapshot(StartingTilePresets tiles) =>
        tiles.Relative.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
}
