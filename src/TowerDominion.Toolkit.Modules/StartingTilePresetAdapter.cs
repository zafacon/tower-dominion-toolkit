namespace TowerDominion.Toolkit.Modules;

/// <summary>Named starting-tile snapshots using the existing absolute/relative preset format.</summary>
public sealed class StartingTilePresetAdapter
{
    private readonly StartingTilePresets selections;

    public StartingTilePresetAdapter(StartingTilePresets selections) =>
        this.selections = selections ?? throw new ArgumentNullException(nameof(selections));

    public string[] Snapshot() =>
        new[] { $"active-name:{selections.ActiveName ?? ""}" }
            .Concat(selections.Active is null ? Array.Empty<string>() :
                selections.Active.Values.Select(StartingTilePresets.Format).OrderBy(x => x, StringComparer.Ordinal))
            .Concat(selections.Relative.OrderBy(entry => entry.Key)
                .Select(entry => $"relative-D{entry.Key}:{string.Join(",", entry.Value)}"))
            .ToArray();

    public CommandPreview PreviewCurrent() => StartingTilePreview.Create(selections);
    public IReadOnlyList<string> Names() => selections.Names();
    public void Save(string name) => selections.Save(name);
    public string Inspect(string name) => selections.DescribeSaved(name);
    public int Load(string name) => selections.Load(name);
    public bool Delete(string name) => selections.Delete(name);
}
