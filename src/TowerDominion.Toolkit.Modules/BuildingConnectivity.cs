namespace TowerDominion.Toolkit.Modules;

/// <summary>Grid-only HQ connectivity; independent of Unity and IL2CPP.</summary>
public static class BuildingConnectivity
{
    public readonly record struct Cell(int X, int Y);

    /// <summary>Returns whether the proposed building touches the HQ-connected component.
    /// Each existing footprint belongs to one building, not one independent tile.</summary>
    public static bool CanPlace(IReadOnlyCollection<Cell> candidate,
        IReadOnlyCollection<Cell> headquarters,
        IEnumerable<IReadOnlyCollection<Cell>> buildings)
    {
        if (candidate.Count == 0 || headquarters.Count == 0) return false;
        var occupied = new HashSet<Cell>(headquarters);
        var remaining = buildings.Select(b => new HashSet<Cell>(b)).Where(b => b.Count > 0).ToList();
        if (candidate.Any(occupied.Contains) || remaining.Any(b => candidate.Any(b.Contains))) return false;
        bool changed;
        do
        {
            changed = false;
            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                if (!Touches(remaining[i], occupied)) continue;
                occupied.UnionWith(remaining[i]);
                remaining.RemoveAt(i);
                changed = true;
            }
        } while (changed);
        return !candidate.Any(occupied.Contains) && Touches(candidate, occupied);
    }

    private static bool Touches(IEnumerable<Cell> source, HashSet<Cell> destination)
    {
        foreach (Cell cell in source)
            if (destination.Contains(new(cell.X - 1, cell.Y)) ||
                destination.Contains(new(cell.X + 1, cell.Y)) ||
                destination.Contains(new(cell.X, cell.Y - 1)) ||
                destination.Contains(new(cell.X, cell.Y + 1))) return true;
        return false;
    }
}
