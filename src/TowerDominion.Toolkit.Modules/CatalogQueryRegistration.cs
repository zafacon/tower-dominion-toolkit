namespace TowerDominion.Toolkit.Modules;

/// <summary>Registers catalog and selection queries and whole-domain resets.</summary>
public static class CatalogQueryRegistration
{
    public static void Register(CommandGrammar grammar, CommandCatalogService catalog,
        BanSelections bans, FrontierSelections frontiers)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(bans);
        ArgumentNullException.ThrowIfNull(frontiers);
        foreach (string domain in BanSelections.Categories)
        {
            string current = domain;
            grammar.Register(new("list", current, $"List {current}", Array.Empty<ArgumentDefinition>(),
                Handler: _ => ListCatalog(catalog, current)));
            grammar.Register(new("list", current, $"List banned {current}", Array.Empty<ArgumentDefinition>(),
                Qualifier: "banned", Handler: _ => ListBanned(catalog, bans, current)));
        }
        grammar.Register(new("list", "frontiers", "List Frontier modifiers", Array.Empty<ArgumentDefinition>(),
            Handler: _ => ListCatalog(catalog, "frontiers")));
        grammar.Register(new("list", "frontiers", "List selected Frontier modifiers", Array.Empty<ArgumentDefinition>(),
            Qualifier: "selected", Handler: _ => ListSelected(catalog, frontiers)));
        grammar.Register(new("clear", "frontiers", "Clear selected Frontier modifiers", Array.Empty<ArgumentDefinition>(),
            Handler: _ => ClearFrontiers(frontiers)));
        grammar.RegisterDomain(new("bans", "All current bans"));
        grammar.Register(new("list", "bans", "List bans across categories", Array.Empty<ArgumentDefinition>(),
            Handler: _ => ListAllBans(catalog, bans)));
        grammar.Register(new("clear", "bans", "Clear all bans", Array.Empty<ArgumentDefinition>(),
            Handler: _ => ClearBans(bans)));
    }

    private static CommandOutcome ClearFrontiers(FrontierSelections frontiers)
    {
        string[] before = frontiers.SelectedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        frontiers.Change(before, false);
        return CommandOutcome.Ok("Frontier selections cleared.", new CommandResultDetails(
            Data: new Dictionary<string, object?> { ["cleared"] = before.Length },
            Changes: before.Length == 0 ? Array.Empty<CommandStateChange>() :
                new[] { new CommandStateChange("frontiers", "selected", before, Array.Empty<string>()) }));
    }

    private static CommandOutcome ClearBans(BanSelections bans)
    {
        var before = BanSelections.Categories.ToDictionary(domain => domain,
            domain => bans.List(domain).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
        bans.Replace(new Dictionary<string, string[]>());
        var changes = before.Where(entry => entry.Value.Length > 0)
            .Select(entry => new CommandStateChange(entry.Key, "banned", entry.Value, Array.Empty<string>()))
            .ToArray();
        return CommandOutcome.Ok("Bans cleared.", new CommandResultDetails(
            Data: new Dictionary<string, object?> { ["cleared"] = before.Sum(entry => entry.Value.Length) },
            Changes: changes));
    }

    private static CommandOutcome ListCatalog(CommandCatalogService catalog, string domain)
    {
        var entries = catalog.List(domain);
        return entries is null ? CommandOutcome.Failure($"Catalog unavailable for {domain}.", CommandErrorKind.Unavailable)
            : CommandOutcome.Ok($"{domain} ({entries.Count}): " + string.Join(", ", entries.Select(x => x.Name)));
    }

    private static CommandOutcome ListBanned(CommandCatalogService catalog, BanSelections bans, string domain)
    {
        var entries = catalog.List(domain);
        if (entries is null) return CommandOutcome.Failure($"Catalog unavailable for {domain}.", CommandErrorKind.Unavailable);
        var names = bans.List(domain).Select(id => entries.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id);
        return CommandOutcome.Ok($"Banned {domain}: " + string.Join(", ", names));
    }

    private static CommandOutcome ListSelected(CommandCatalogService catalog, FrontierSelections frontiers)
    {
        var entries = catalog.List("frontiers");
        if (entries is null) return CommandOutcome.Failure("Catalog unavailable for frontiers.", CommandErrorKind.Unavailable);
        var names = frontiers.SelectedIds.Select(id => entries.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id);
        return CommandOutcome.Ok("Selected frontiers: " + string.Join(", ", names));
    }

    private static CommandOutcome ListAllBans(CommandCatalogService catalog, BanSelections bans)
    {
        var lines = new List<string>();
        foreach (string domain in BanSelections.Categories)
        {
            var entries = catalog.List(domain);
            foreach (string id in bans.List(domain))
            {
                string name = entries?.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id;
                lines.Add($"{domain}: {name}");
            }
        }
        return CommandOutcome.Ok("Bans: " + string.Join(", ", lines));
    }
}
