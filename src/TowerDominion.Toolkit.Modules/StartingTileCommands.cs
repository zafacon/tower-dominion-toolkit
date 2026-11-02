namespace TowerDominion.Toolkit.Modules;

/// <summary>Starting-tile command surface; game hooks are installed separately.</summary>
public sealed class StartingTileCommands
{
    private readonly StartingTilePresets presets;
    public StartingTileCommands(StartingTilePresets presets) => this.presets = presets;

    public string Execute(string input)
    {
        string trimmed = input.Trim();
        try
        {
            if (trimmed.StartsWith("set relative tiles ", StringComparison.OrdinalIgnoreCase))
            {
                string rest = trimmed[19..].Trim();
                int separator = rest.IndexOf(' ');
                if (separator < 0 || !int.TryParse(rest[..separator].TrimStart('D', 'd'), out int difficulty))
                    return "Usage: set relative tiles D1|D2|D3|D4 <shapes>";
                presets.SetRelative(difficulty, rest[(separator + 1)..]);
                return $"D{difficulty} relative tiles saved: {string.Join(",", presets.Relative[difficulty])}";
            }
            if (trimmed.StartsWith("clear starting tiles ", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(trimmed[21..].Trim().TrimStart('D', 'd'), out int difficulty) || difficulty is < 1 or > 4)
                    return "Usage: clear starting tiles 1|2|3|4";
                presets.ClearRelative(difficulty);
                return $"D{difficulty} starting tiles cleared.";
            }
            if (trimmed.StartsWith("clear relative tiles ", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(trimmed[21..].Trim().TrimStart('D', 'd'), out int difficulty) || difficulty is < 1 or > 4)
                    return "Usage: clear relative tiles D1|D2|D3|D4";
                presets.ClearRelative(difficulty);
                return $"D{difficulty} relative tiles cleared.";
            }
            if (trimmed.Equals("list relative tiles", StringComparison.OrdinalIgnoreCase))
                return presets.Relative.Count == 0 ? "No relative tiles configured." :
                    string.Join("; ", presets.Relative.OrderBy(p => p.Key).Select(p => $"D{p.Key}: {string.Join(",", p.Value)}"));
            if (trimmed.StartsWith("set starting tiles ", StringComparison.OrdinalIgnoreCase))
            {
                string spec = trimmed[19..];
                if (spec.Contains(':'))
                {
                    presets.Set(spec); // Compatibility with existing compass presets.
                    return "Starting tiles set: " + Describe(presets.Active!);
                }
                presets.SetRelative(spec);
                int count = spec.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                return $"D{count} starting tiles saved: {string.Join(",", presets.Relative[count])}";
            }
            if (trimmed.StartsWith("activate tileset ", StringComparison.OrdinalIgnoreCase))
            {
                string name = trimmed[17..].Trim();
                presets.Activate(name);
                return $"Active tileset '{name}' will be restored on restart.";
            }
            if (trimmed.Equals("deactivate tileset", StringComparison.OrdinalIgnoreCase))
            {
                presets.Clear();
                return "Active tileset deactivated.";
            }
            if (trimmed.StartsWith("save starting tiles as ", StringComparison.OrdinalIgnoreCase))
            {
                string name = trimmed[23..].Trim();
                presets.Save(name);
                return $"Saved starting-tile preset '{name}'.";
            }
            if (trimmed.StartsWith("load starting tiles ", StringComparison.OrdinalIgnoreCase))
            {
                string name = trimmed[20..].Trim();
                presets.Load(name);
                return $"Loaded starting-tile preset '{name}'.";
            }
            if (trimmed.Equals("unload starting tiles", StringComparison.OrdinalIgnoreCase))
            {
                presets.Clear();
                return "Active starting tiles cleared.";
            }
            if (trimmed.StartsWith("delete starting-tile-preset ", StringComparison.OrdinalIgnoreCase))
            {
                string name = trimmed[28..].Trim();
                return presets.Delete(name) ? $"Deleted starting-tile preset '{name}'." : $"No starting-tile preset named '{name}'.";
            }
            if (trimmed.Equals("list starting-tiles", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("list starting tiles", StringComparison.OrdinalIgnoreCase))
                return (presets.Relative.Count == 0 ? "No relative tiles configured." : string.Join("; ", presets.Relative.OrderBy(p => p.Key).Select(p => $"D{p.Key}: {string.Join(",", p.Value)}"))) +
                    (presets.Active == null ? "" : " | Legacy: " + Describe(presets.Active));
            if (trimmed.Equals("list starting-tile-presets", StringComparison.OrdinalIgnoreCase))
                return "Saved starting-tile presets: " + string.Join(", ", presets.Names());
            if (trimmed.StartsWith("list starting-tile-preset ", StringComparison.OrdinalIgnoreCase))
            {
                string name = trimmed[26..].Trim();
                return $"Starting-tile preset '{name}': " + presets.DescribeSaved(name);
            }
        }
        catch (FileNotFoundException) { return "Starting-tile preset not found."; }
        catch (DirectoryNotFoundException) { return "Starting-tile preset not found."; }
        catch (ArgumentException ex) { return ex.Message; }
        catch (InvalidOperationException ex) { return ex.Message; }
        catch (InvalidDataException ex) { return ex.Message; }
        return "Usage: set starting tiles N:S,E:X,S:S,W:S | save starting tiles as <name> | activate tileset <name> | deactivate tileset | load/unload starting tiles | delete starting-tile-preset <name> | list starting-tiles | list starting-tile-presets | list starting-tile-preset <name>";
    }

    private static string Describe(IReadOnlyDictionary<StartPosition, StartingTile> tiles) =>
        string.Join(", ", Enum.GetValues<StartPosition>().Select(p => StartingTilePresets.Format(tiles[p])));
}
