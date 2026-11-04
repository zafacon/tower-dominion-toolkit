namespace TowerDominion.Toolkit.Modules;

/// <summary>Pure cap for effective recurring production, preserving reductions.</summary>
public static class ProductionCap
{
    public static long Apply(long effectiveProduction, long startingProduction) =>
        Math.Min(effectiveProduction, startingProduction);
}
