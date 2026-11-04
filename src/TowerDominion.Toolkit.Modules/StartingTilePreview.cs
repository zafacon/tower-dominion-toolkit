namespace TowerDominion.Toolkit.Modules;

/// <summary>Immutable snapshots for rendering starting-tile command outcomes.</summary>
public static class StartingTilePreview
{
    public static CommandPreview Create(StartingTilePresets tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        var relative = tiles.Relative.ToDictionary(pair => $"D{pair.Key}", pair => pair.Value.ToArray());
        var absolute = tiles.Active?.Values.Select(StartingTilePresets.Format).ToArray() ?? Array.Empty<string>();
        return new("starting tiles", new Dictionary<string, object?>
        {
            ["relative"] = relative,
            ["absolute"] = absolute,
            ["activeName"] = tiles.ActiveName
        });
    }
}
