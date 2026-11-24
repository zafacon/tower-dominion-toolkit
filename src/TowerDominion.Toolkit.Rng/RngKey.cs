namespace TowerDominion.Toolkit.Rng;

public readonly record struct RngKey(string Scope, string Domain, string Stream)
{
    public override string ToString() => $"{Scope}/{Domain}/{Stream}";
}
