using System.Text.Json;

namespace TowerDominion.Toolkit.Modules;

public sealed class StartingTilePresets
{
    private readonly string directory;
    private IReadOnlyDictionary<StartPosition, StartingTile>? active;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private string RelativePath => Path.Combine(directory, "relative-selection.json");
    private readonly Dictionary<int, string[]> relative = new();
    public IReadOnlyDictionary<int, string[]> Relative => relative;
    private string RelativePresetPath(string name) => PathFor(name);
    private string SelectionPath => Path.Combine(directory, "active-selection.txt");
    public string? ActiveName { get; private set; }
    public StartingTilePresets(string directory)
    {
        this.directory = directory;
        if (File.Exists(RelativePath))
        {
            try
            {
                var saved = JsonSerializer.Deserialize<Dictionary<int, string[]>>(File.ReadAllText(RelativePath));
                if (saved != null)
                    foreach (var pair in saved) relative[pair.Key] = RelativeStartingTiles.Parse(pair.Key, string.Join(",", pair.Value)).ToArray();
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or JsonException) { relative.Clear(); }
        }
        if (!File.Exists(SelectionPath)) return;
        var name = File.ReadAllText(SelectionPath).Trim();
        try { Activate(name); }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidDataException)
        {
            // A removed or invalid preset must never prevent the toolkit from loading.
            ActiveName = null;
            active = null;
        }
    }
    public IReadOnlyDictionary<StartPosition, StartingTile>? Active => active;
    public void SetRelative(string shapes) => SetRelative(shapes.Split(',', StringSplitOptions.RemoveEmptyEntries).Length, shapes);
    public void SetRelative(int difficulty, string shapes)
    {
        var parsed = RelativeStartingTiles.Parse(difficulty, shapes).ToArray();
        relative[difficulty] = parsed;
        Directory.CreateDirectory(directory);
        File.WriteAllText(RelativePath, JsonSerializer.Serialize(relative, Options));
    }
    public void ClearRelative(int difficulty)
    {
        relative.Remove(difficulty);
        if (relative.Count == 0) { if (File.Exists(RelativePath)) File.Delete(RelativePath); }
        else File.WriteAllText(RelativePath, JsonSerializer.Serialize(relative, Options));
    }
    public bool TryGetRelative(int difficulty, IEnumerable<StartPosition> entrances, out IReadOnlyDictionary<StartPosition, StartingTile> tiles)
    {
        tiles = null!;
        if (!relative.TryGetValue(difficulty, out var shapes)) return false;
        tiles = RelativeStartingTiles.Map(difficulty, shapes, entrances);
        return true;
    }
    public void Set(string specification)
    {
        var parsed = StartingTiles.ParseSet(specification);
        active = parsed;
        ActiveName = null;
        if (File.Exists(SelectionPath)) File.Delete(SelectionPath);
    }
    public void Clear()
    {
        active = null;
        ActiveName = null;
        if (File.Exists(SelectionPath)) File.Delete(SelectionPath);
    }
    public void Activate(string name)
    {
        var parsed = ReadOptionalAbsolute(name);
        Directory.CreateDirectory(directory);
        string temporary = SelectionPath + ".tmp";
        try
        {
            File.WriteAllText(temporary, name);
            File.Move(temporary, SelectionPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        active = parsed;
        ActiveName = name;
    }
    public bool TryGet(int x, int y, out StartingTile tile)
    {
        tile = null!;
        if (active == null) return false;
        StartPosition position;
        if (x == 0 && y == 2) position = StartPosition.N;
        else if (x == 2 && y == 0) position = StartPosition.E;
        else if (x == 0 && y == -2) position = StartPosition.S;
        else if (x == -2 && y == 0) position = StartPosition.W;
        else return false;
        return active.TryGetValue(position, out tile!);
    }
    public static string Format(StartingTile tile)
    {
        if (tile.Shape is StartShape.S or StartShape.X) return $"{tile.Position}:{tile.Shape}";
        var hq = StartingTiles.HqFacing(tile.Position);
        var cw = StartingTiles.TurnClockwise(hq);
        if (tile.Shape == StartShape.L) return $"{tile.Position}:L.{((tile.Paths & cw) != 0 ? "C" : "A")}";
        if ((tile.Paths & StartingTiles.TurnClockwise(hq)) != 0 &&
            (tile.Paths & StartingTiles.TurnAnticlockwise(hq)) != 0) return $"{tile.Position}:T";
        // T.C/T.A use the outward-facing convention of the original parser.
        return $"{tile.Position}:T.{((tile.Paths & cw) != 0 ? "A" : "C")}";
    }
    private string PathFor(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name is "." or ".." || name.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_' or ' ')))
            throw new ArgumentException("Invalid starting-tile preset name.");
        return Path.Combine(directory, name + ".json");
    }
    public IReadOnlyList<string> Names() => Directory.Exists(directory)
        ? Directory.GetFiles(directory, "*.json").Select(Path.GetFileNameWithoutExtension).Where(x => x != null && x != "relative-selection").Select(x => x!).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray()
        : Array.Empty<string>();
    private IReadOnlyDictionary<StartPosition, StartingTile>? ReadOptionalAbsolute(string name)
    {
        var json = File.ReadAllText(PathFor(name));
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("relative", out var entries))
        {
            var saved = JsonSerializer.Deserialize<Dictionary<int, string[]>>(entries.GetRawText())!;
            if (saved == null) throw new InvalidDataException("Invalid relative starting-tile preset.");
            var validated = saved.ToDictionary(pair => pair.Key,
                pair => RelativeStartingTiles.Parse(pair.Key, string.Join(",", pair.Value)).ToArray());
            PersistRelative(validated);
            relative.Clear();
            foreach (var pair in validated) relative[pair.Key] = pair.Value;
            return null;
        }
        return StartingTiles.ParseSet(string.Join(",", JsonSerializer.Deserialize<Dictionary<string, string>>(json)!.Values));
    }
    private void PersistRelative(IReadOnlyDictionary<int, string[]> snapshot)
    {
        Directory.CreateDirectory(directory);
        string temporary = RelativePath + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot, Options));
            File.Move(temporary, RelativePath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public IReadOnlyDictionary<int, string[]> PreviewSavedRelative(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(PathFor(name)));
        if (!doc.RootElement.TryGetProperty("relative", out var entry)) return new Dictionary<int, string[]>();
        return JsonSerializer.Deserialize<Dictionary<int, string[]>>(entry.GetRawText()) ?? new Dictionary<int, string[]>();
    }
    public string DescribeSaved(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(PathFor(name)));
        if (doc.RootElement.TryGetProperty("relative", out var entry))
        {
            var values = JsonSerializer.Deserialize<Dictionary<int, string[]>>(entry.GetRawText())!;
            return string.Join("; ", values.OrderBy(x => x.Key).Select(x => $"D{x.Key}: {string.Join(",", x.Value)}"));
        }
        return string.Join(", ", Read(name).Values.Select(Format));
    }
    public IReadOnlyDictionary<StartPosition, StartingTile> Read(string name)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(PathFor(name))) ?? throw new InvalidDataException("Invalid starting-tile preset.");
        return StartingTiles.ParseSet(string.Join(",", data.Values));
    }
    public void Save(string name)
    {
        if (active == null && relative.Count == 0) throw new InvalidOperationException("No active starting-tile configuration.");
        string path = PathFor(name);
        Directory.CreateDirectory(directory);
        string temp = path + ".tmp";
        try
        {
            File.WriteAllText(temp, relative.Count > 0 ? JsonSerializer.Serialize(new { relative }, Options) : JsonSerializer.Serialize(active!.ToDictionary(x => x.Key.ToString(), x => Format(x.Value)), Options));
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public int Load(string name)
    {
        var parsed = ReadOptionalAbsolute(name);
        active = parsed;
        ActiveName = null;
        if (File.Exists(SelectionPath)) File.Delete(SelectionPath);
        return parsed?.Count ?? relative.Count;
    }
    public bool Delete(string name)
    {
        var path = PathFor(name);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        if (string.Equals(ActiveName, name, StringComparison.OrdinalIgnoreCase)) Clear();
        return true;
    }
}
