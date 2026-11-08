namespace TowerDominion.Toolkit.Modules;

/// <summary>Pure choice-slot planning; native runtime must separately validate eligibility.</summary>
public static class PinChoicePlanner
{
    public static int FindReplacement<T>(
        IReadOnlyList<T> selected, IReadOnlyCollection<string> pinnedIds,
        Func<T, string> id, Func<T, bool> mandatory)
    {
        for (int i = selected.Count - 1; i >= 0; i--)
            if (!mandatory(selected[i]) &&
                !pinnedIds.Contains(id(selected[i]), StringComparer.OrdinalIgnoreCase))
                return i;
        return -1;
    }
}
