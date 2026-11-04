using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

internal static class GameCommandComposition
{
    internal static CommandGrammar Create(ICommandCatalogSource source, BanSelections bans,
        FrontierSelections frontiers, ChallengeRegistry registry, ChallengeSelections challenges,
        StartingTilePresets tiles, ISeedService seeds, Func<string> status, string directory)
    {
        var grammar = new CommandGrammar();
        var catalog = new CommandCatalogService(source);
        CatalogSelectionRegistration.Register(grammar, catalog,
            new CatalogSelectionHandlers(new CatalogSelectionService(catalog), bans, frontiers));
        CatalogQueryRegistration.Register(grammar, catalog, bans, frontiers);
        ChallengeCommandRegistration.Register(grammar, registry, challenges);
        StartingTileCommandRegistration.Register(grammar, tiles);
        RuntimeCommandRegistration.Register(grammar, seeds, () => FrontierDiagnostics.Enabled, enabled => FrontierDiagnostics.Enabled = enabled, status);
        PresetCommandRegistration.Register(grammar,
            new SelectionPresetAdapters(Path.Combine(directory, "selections"), bans, frontiers),
            new ChallengePresetAdapter(Path.Combine(directory, "challenges"), registry, challenges),
            new StartingTilePresetAdapter(tiles));
        return grammar;
    }
}
