namespace TowerDominion.Toolkit.Rng;

public static class NativeRunSeed
{
    /// <summary>
    /// Preserves the exact 32-bit pattern of Tower Dominion's signed native seed.
    /// </summary>
    public static ulong ToRootSeed(int nativeSeed) => unchecked((uint)nativeSeed);
}
