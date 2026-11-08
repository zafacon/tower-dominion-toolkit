namespace TowerDominion.Toolkit.Modules;

/// <summary>Registers canonical ban and Frontier mutations with typed invocation handlers.</summary>
public static class CatalogSelectionRegistration
{
    public static void Register(CommandGrammar grammar, CommandCatalogService catalog, CatalogSelectionHandlers handlers)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(handlers);
        foreach (string domain in BanSelections.Categories)
        {
            grammar.RegisterDomain(new(domain, $"{domain} catalog and bans"));
            RegisterMutation(grammar, catalog, domain, "ban", invocation => handlers.Ban(domain,
                Members(invocation), IsAll(invocation), true));
            RegisterMutation(grammar, catalog, domain, "unban", invocation => handlers.Ban(domain,
                Members(invocation), IsAll(invocation), false));
        }
        foreach (string domain in PinSelections.Categories)
        {
            RegisterMutation(grammar, catalog, domain, "pin", invocation => handlers.Pin(domain,
                Members(invocation), IsAll(invocation), true));
            RegisterMutation(grammar, catalog, domain, "unpin", invocation => handlers.Pin(domain,
                Members(invocation), IsAll(invocation), false));
        }
        grammar.RegisterDomain(new("frontiers", "Frontier modifiers"));
        RegisterMutation(grammar, catalog, "frontiers", "select", invocation => handlers.Frontier(
            Members(invocation), IsAll(invocation), true));
        RegisterMutation(grammar, catalog, "frontiers", "unselect", invocation => handlers.Frontier(
            Members(invocation), IsAll(invocation), false));
    }

    private static void RegisterMutation(CommandGrammar grammar, CommandCatalogService catalog,
        string domain, string verb, Func<CommandInvocation, CommandOutcome> handler)
    {
        string description = domain switch
        {
            "foes" when verb is "ban" or "unban" => "Exclude or restore regular-wave foe candidates from the native encounter catalog.",
            "foes" when verb is "pin" or "unpin" => "Request a regular-wave foe from the same native catalog used by bans, without extra choice slots. Pins are session-only; use presets to save them.",
            "waves" when verb is "pin" or "unpin" => "Request an eligible final-wave encounter among choices; unavailable final waves are not forced. Pins are session-only.",
            _ => $"{verb} {domain} entries"
        };
        grammar.Register(new(verb, domain, description,
            new[] { new ArgumentDefinition("names", "Comma-separated names, IDs or aliases; or all",
                Completion: ArgumentCompletion.Catalog, CommaSeparated: true,
                CatalogProvider: () => CatalogCandidates(catalog, domain, includeIdentifiers: true),
                CompletionProvider: () => CatalogCandidates(catalog, domain, includeIdentifiers: false)) }, Handler: handler));
    }

    private static string[]? CatalogCandidates(CommandCatalogService catalog, string domain, bool includeIdentifiers)
    {
        var entries = catalog.List(domain);
        if (entries is null) return null;
        // Keep full accepted identifiers for validation, but suggest readable names only.
        IEnumerable<string> candidates = includeIdentifiers
            ? entries.SelectMany(x => new[] { x.Name, x.Id }.Concat(x.Aliases))
            : entries.Where(x => !Guid.TryParse(x.Name, out _))
                .Select(x => x.Name);
        return candidates.Append("all").Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IReadOnlyList<string> Members(CommandInvocation invocation) =>
        invocation.SelectionMembers is not null && invocation.SelectionMembers.TryGetValue("names", out var names)
            ? names : Array.Empty<string>();

    private static bool IsAll(CommandInvocation invocation) =>
        invocation.AllSelections?.Contains("names", StringComparer.OrdinalIgnoreCase) ?? false;
}
