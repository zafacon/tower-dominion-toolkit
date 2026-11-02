namespace TowerDominion.Toolkit.Model;

/// <summary>Initial shared model; game-specific state is added when a module requires it.</summary>
public sealed class GameModel : IGameModel
{
    public GameModel(ICapabilityRegistry capabilities)
    {
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public ICapabilityRegistry Capabilities { get; }
}
