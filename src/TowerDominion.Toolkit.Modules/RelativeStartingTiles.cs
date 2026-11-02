namespace TowerDominion.Toolkit.Modules;

/// <summary>Difficulty-specific shapes mapped to actual HQ exits, not absolute compass presets.</summary>
public static class RelativeStartingTiles
{
    private static readonly StartPosition[] Clockwise =
        { StartPosition.N, StartPosition.E, StartPosition.S, StartPosition.W };

    public static IReadOnlyList<string> Parse(int difficulty, string specification)
    {
        if (difficulty is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(difficulty));
        var tokens = specification.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length != difficulty) throw new ArgumentException($"D{difficulty} requires {difficulty} tile shapes.");
        var result = new List<string>();
        foreach (var token in tokens)
        {
            string shape = token.Trim().ToUpperInvariant();
            // Use the existing parser to validate shape and orientation semantics.
            StartingTiles.Parse("N:" + shape);
            result.Add(shape);
        }
        return result;
    }

    public static IReadOnlyDictionary<StartPosition, StartingTile> Map(
        int difficulty, IReadOnlyList<string> shapes, IEnumerable<StartPosition> activeEntrances)
    {
        if (difficulty is < 1 or > 4 || shapes.Count != difficulty)
            throw new ArgumentException("Invalid difficulty or shape count.");
        var active = activeEntrances.Distinct().ToHashSet();
        if (active.Count != difficulty) throw new ArgumentException("Entrance count does not match difficulty.");
        StartPosition[] ordered;
        if (difficulty == 1) ordered = active.ToArray();
        else if (difficulty == 2)
        {
            // Only opposing pairs have an orientation-independent canonical axis.
            if (active.SetEquals(new[] { StartPosition.N, StartPosition.S }))
                ordered = new[] { StartPosition.N, StartPosition.S };
            else if (active.SetEquals(new[] { StartPosition.E, StartPosition.W }))
                ordered = new[] { StartPosition.E, StartPosition.W };
            else throw new ArgumentException("D2 requires opposing HQ entrances.");
        }
        else if (difficulty == 4) ordered = Clockwise;
        else
        {
            var missing = Clockwise.Single(p => !active.Contains(p));
            int index = Array.IndexOf(Clockwise, missing);
            ordered = new[] { Clockwise[(index + 1) % 4], Clockwise[(index + 2) % 4], Clockwise[(index + 3) % 4] };
        }
        return ordered.Select((position, i) => StartingTiles.Parse(position + ":" + shapes[i]))
            .ToDictionary(tile => tile.Position);
    }
}
