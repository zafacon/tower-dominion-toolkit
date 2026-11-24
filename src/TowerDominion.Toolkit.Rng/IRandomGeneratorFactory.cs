namespace TowerDominion.Toolkit.Rng;

public interface IRandomGeneratorFactory
{
    string AlgorithmId { get; }
    IRandomGenerator Create(ulong seed);
}
