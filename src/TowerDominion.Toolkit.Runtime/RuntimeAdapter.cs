using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Runtime;

public interface IRuntimeAdapter
{
    void Initialize(IGameModel model);
}
