namespace TowerDominion.Toolkit.Model;

/// <summary>Game-neutral snapshot of discoverable content, keyed by stable game identifiers.</summary>
public sealed record CatalogEntity(string Id, string Name, IReadOnlyCollection<string> Aliases);

public sealed class EntityCatalog
{
    private readonly Dictionary<string, IReadOnlyList<CatalogEntity>> categories = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Categories => categories.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

    public void Replace(string category, IEnumerable<CatalogEntity> entities)
    {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        ArgumentNullException.ThrowIfNull(entities);
        CatalogEntity[] snapshot = entities.ToArray();
        if (snapshot.Any(x => string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Name)))
            throw new ArgumentException("Catalog entities require an ID and name.", nameof(entities));
        if (snapshot.Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != snapshot.Length)
            throw new ArgumentException("Duplicate entity IDs in category.", nameof(entities));
        categories[category] = snapshot;
    }

    public IReadOnlyList<CatalogEntity> List(string category) => categories.TryGetValue(category, out var entities)
        ? entities : Array.Empty<CatalogEntity>();

    public bool TryResolve(string category, string nameOrId, out CatalogEntity? entity)
    {
        entity = null;
        var matches = List(category).Where(x => x.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)
            || x.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)
            || x.Aliases.Any(alias => alias.Equals(nameOrId, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (matches.Length != 1) return false;
        entity = matches[0];
        return true;
    }
}
