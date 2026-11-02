using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

public interface ICapabilityModule
{
    string Id { get; }

    IReadOnlyCollection<string> RequiredCapabilities { get; }

    void Initialize(IGameModel model);
}

public interface IModuleConfiguration
{
    bool IsEnabled(string moduleId);
}
