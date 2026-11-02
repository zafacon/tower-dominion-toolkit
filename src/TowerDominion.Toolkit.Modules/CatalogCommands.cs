using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Game-neutral listing of content snapshots supplied by the game adapter.</summary>
public sealed class CatalogCommands
{
    private static readonly string[] targets = { "foes", "waves", "blueprints", "doctrines", "frontiers" };
    private readonly EntityCatalog catalog;
    private readonly Func<bool> isReady;

    public CatalogCommands(EntityCatalog catalog, Func<bool> isReady)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.isReady = isReady ?? throw new ArgumentNullException(nameof(isReady));
    }

    public string Execute(string command)
    {
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !parts[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            return "Usage: list <foes|waves|blueprints|doctrines|frontiers>";
        if (!targets.Contains(parts[1], StringComparer.OrdinalIgnoreCase))
            return "Usage: list <foes|waves|blueprints|doctrines|frontiers>";
        if (!isReady()) return "Game catalog is not ready yet. Try again after the game finishes loading.";
        var entries = catalog.List(parts[1]).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        return $"{parts[1]} ({entries.Length}):" + (entries.Length == 0 ? " (none)" : Environment.NewLine + string.Join(Environment.NewLine, entries.Select(x => x.Name)));
    }
}
