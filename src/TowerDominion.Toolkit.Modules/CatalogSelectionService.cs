namespace TowerDominion.Toolkit.Modules;

/// <summary>Atomic catalog-backed selection for bans and Frontier modifiers.</summary>
public sealed class CatalogSelectionService
{
    private readonly CommandCatalogService catalog;
    public CatalogSelectionService(CommandCatalogService catalog) =>
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

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

    public CatalogSelectionHandlers(CatalogSelectionService resolver, BanSelections bans, FrontierSelections frontiers)
    {
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.bans = bans ?? throw new ArgumentNullException(nameof(bans));
        this.frontiers = frontiers ?? throw new ArgumentNullException(nameof(frontiers));
    }

    public CommandOutcome Ban(string domain, IReadOnlyList<string> names, bool all, bool add) =>
        resolver.Change(domain, names, all, ids =>
        {
            string[] before = bans.List(domain).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
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
