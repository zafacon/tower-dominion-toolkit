using System.Text;

namespace TowerDominion.Toolkit.Rng;

public sealed class Fnv1aKeyDeriver : IRngKeyDeriver
{
    public string DerivationId => "fnv1a64+splitmix64-v1";

    public ulong Derive(ulong rootSeed, RngKey key)
    {
        ulong hash = 14695981039346656037UL;
        Add(ref hash, key.Scope);
        AddByte(ref hash, 0);
        Add(ref hash, key.Domain);
        AddByte(ref hash, 0);
        Add(ref hash, key.Stream);

        ulong mixed = rootSeed ^ hash;
        return SplitMix64.Next(ref mixed);
    }

    private static void Add(ref ulong hash, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        foreach (byte b in Encoding.UTF8.GetBytes(value))
            AddByte(ref hash, b);
    }

    private static void AddByte(ref ulong hash, byte value)
    {
        hash ^= value;
        hash *= 1099511628211UL;
    }
}
