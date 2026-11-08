namespace TowerDominion.Toolkit.Modules;

/// <summary>Atomic catalog-backed selection for bans and Frontier modifiers.</summary>
public sealed class CatalogSelectionService
{
    private readonly CommandCatalogService catalog;
    public CatalogSelectionService(CommandCatalogService catalog) =>
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public string DisplayName(string domain, string id) =>
        catalog.List(domain)?.FirstOrDefault(entry =>
            entry.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id;

    public CommandOutcome Change(string domain, IReadOnlyList<string> names, bool all,
        Func<IReadOnlyList<string>, CommandOutcome> apply)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(apply);
        if (all && names.Count != 0)
            return CommandOutcome.Failure("All cannot be combined with individual selections.", CommandErrorKind.Validation);
        if (!all && (names.Count == 0 || names.Any(string.IsNullOrWhiteSpace)))
            return CommandOutcome.Failure("At least one valid selection is required.", CommandErrorKind.Validation);
        var snapshot = catalog.List(domain);
        if (snapshot is null) return CommandOutcome.Failure($"Catalog unavailable for {domain}.", CommandErrorKind.Unavailable);
        var ids = new List<string>();
        if (all) ids.AddRange(snapshot.Select(entry => entry.Id));
        else
        {
            foreach (string name in names)
            {
                var matches = snapshot.Where(entry =>
                    entry.Id.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    entry.Aliases.Any(alias => alias.Equals(name, StringComparison.OrdinalIgnoreCase))).ToArray();
                if (matches.Length != 1)
                    return CommandOutcome.Failure(matches.Length == 0
                        ? $"Unknown {domain} entry: {name}." : $"Ambiguous {domain} entry: {name}.", CommandErrorKind.Validation);
                ids.Add(matches[0].Id);
            }
        }
        return apply(ids.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}

/// <summary>Headless mutation handlers for existing ban and Frontier state stores.</summary>
public sealed class CatalogSelectionHandlers
{
    private readonly CatalogSelectionService resolver;
    private readonly BanSelections bans;
    private readonly FrontierSelections frontiers;
    private readonly PinSelections? pins;

    public CatalogSelectionHandlers(CatalogSelectionService resolver, BanSelections bans, FrontierSelections frontiers, PinSelections? pins = null)
    {
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.bans = bans ?? throw new ArgumentNullException(nameof(bans));
        this.frontiers = frontiers ?? throw new ArgumentNullException(nameof(frontiers));
        this.pins = pins;
    }

    public CommandOutcome Ban(string domain, IReadOnlyList<string> names, bool all, bool add) =>
        resolver.Change(domain, names, all, ids =>
        {
            string[] before = bans.List(domain).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
            if (add && pins is not null && (domain == "foes" || domain == "waves"))
            {
                string? conflict = ids.FirstOrDefault(id => pins.IsPinned(domain, id));
                if (conflict is not null) return CommandOutcome.Failure(
                    $"Cannot ban {resolver.DisplayName(domain, conflict)}: it is pinned. Unpin it first.", CommandErrorKind.Conflict);
            }
            var result = bans.Change(domain, ids, add);
            string[] after = bans.List(domain).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
            var changes = result.Changed == 0 ? Array.Empty<CommandStateChange>() :
                new[] { new CommandStateChange(domain, "banned", before, after) };
            return CommandOutcome.Ok($"{result.Changed} {domain} entries changed; {result.Unchanged} unchanged.",
                new CommandResultDetails(Data: new Dictionary<string, object?>
                {
                    ["changed"] = result.Changed,
                    ["unchanged"] = result.Unchanged,
                    ["selectedIds"] = ids.ToArray()
                }, Changes: changes));
        });

    public CommandOutcome Pin(string domain, IReadOnlyList<string> names, bool all, bool add)
    {
        if (pins is null) return CommandOutcome.Failure("Pins unavailable.", CommandErrorKind.Unavailable);
        return resolver.Change(domain, names, all, ids =>
        {
            if (add)
            {
                string? conflict = ids.FirstOrDefault(id => bans.IsBanned(domain, id));
                if (conflict is not null) return CommandOutcome.Failure(
                    $"Cannot pin {resolver.DisplayName(domain, conflict)}: it is banned. Unban it first.", CommandErrorKind.Conflict);
            }
            var before = pins.List(domain).ToArray();
            var result = pins.Change(domain, ids, add);
            var after = pins.List(domain).ToArray();
            return CommandOutcome.Ok($"{result.Changed} {domain} pins changed; {result.Unchanged} unchanged.",
                new CommandResultDetails(Changes: result.Changed == 0 ? Array.Empty<CommandStateChange>() :
                    new[] { new CommandStateChange(domain, "pinned", before, after) }));
        });
    }

    public CommandOutcome Frontier(IReadOnlyList<string> names, bool all, bool select) =>
        resolver.Change("frontiers", names, all, ids =>
        {
            string[] before = frontiers.SelectedIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
            var result = frontiers.Change(ids, select);
            string[] after = frontiers.SelectedIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
            var changes = result.Changed == 0 ? Array.Empty<CommandStateChange>() :
                new[] { new CommandStateChange("frontiers", "selected", before, after) };
            return CommandOutcome.Ok($"{result.Changed} Frontier selections changed; {result.Unchanged} unchanged.",
                new CommandResultDetails(Data: new Dictionary<string, object?>
                {
                    ["changed"] = result.Changed,
                    ["unchanged"] = result.Unchanged,
                    ["selectedIds"] = ids.ToArray()
                }, Changes: changes));
        });
}
