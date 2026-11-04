using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Game-independent catalog access; runtime adapters supply availability and snapshots.</summary>
public interface ICommandCatalogSource
{
    bool IsReady { get; }
    IReadOnlyList<CatalogEntity> List(string domain);
}

public enum CatalogLookupStatus { Available, Unavailable, Unknown, Ambiguous }

public sealed record CatalogLookupResult(CatalogLookupStatus Status, CatalogEntity? Entity = null,
    IReadOnlyList<CatalogEntity>? Matches = null);

/// <summary>Provides deterministic discovery and exact-name/ID/alias resolution for domains.</summary>
public sealed class CommandCatalogService
{
    private readonly ICommandCatalogSource source;
    public CommandCatalogService(ICommandCatalogSource source) =>
        this.source = source ?? throw new ArgumentNullException(nameof(source));

    public IReadOnlyList<CatalogEntity>? List(string domain) => source.IsReady
        ? source.List(domain).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray()
        : null;

    public IReadOnlyList<string>? CompletionNames(string domain) => List(domain)?
        .Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public CatalogLookupResult Resolve(string domain, string nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId)) throw new ArgumentException("Name or ID is required.", nameof(nameOrId));
        var entries = List(domain);
        if (entries is null) return new(CatalogLookupStatus.Unavailable);
        var matches = entries.Where(x => x.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)
            || x.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)
            || x.Aliases.Any(alias => alias.Equals(nameOrId, StringComparison.OrdinalIgnoreCase))).ToArray();
        return matches.Length switch
        {
            0 => new(CatalogLookupStatus.Unknown),
            1 => new(CatalogLookupStatus.Available, matches[0]),
            _ => new(CatalogLookupStatus.Ambiguous, Matches: matches)
        };
    }
}
