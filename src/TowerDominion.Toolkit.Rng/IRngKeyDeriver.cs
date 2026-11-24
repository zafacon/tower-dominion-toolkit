namespace TowerDominion.Toolkit.Rng;

public interface IRngKeyDeriver
{
    string DerivationId { get; }
    ulong Derive(ulong rootSeed, RngKey key);
}
