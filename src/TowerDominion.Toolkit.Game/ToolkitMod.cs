using MelonLoader;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;
using TowerDominion.Toolkit.Runtime;

[assembly: MelonInfo(typeof(TowerDominion.Toolkit.Game.ToolkitMod), "Tower Dominion Toolkit", ToolkitBuildInfo.Version, "Game Familiar")]
[assembly: MelonGame("Parallel45", "tower-dominion")]

namespace TowerDominion.Toolkit.Game;

public sealed class ToolkitMod : MelonMod
{
    internal static SeedRuntimeBridge? SeedBridge { get; private set; }

    internal static SeedModule? Seeds { get; private set; }

    internal static FrontierSelections? SavedFrontiers { get; private set; }

    internal static BanSelections? SavedBans { get; private set; }
    internal static EntityCatalog? ChallengeCatalog { get; private set; }

    internal static StartingTilePresets? StartingTileSets { get; private set; }

    internal static ChallengeSelections? SavedChallenges { get; private set; }

    internal static ChallengeEnforcement? ChallengeRules { get; private set; }

    private SeedCommandInbox? commandInbox;
    private ToolkitConsoleOverlay? consoleOverlay;
    private ChallengeHudOverlay? challengeHud;
    private ChallengePreRunOverlay? challengePreRun;
    private ChainOfCommandFrontierOverlay? chainFrontier;
    private GameCatalogAdapter? catalogAdapter;
    private CommandGrammar? canonicalGrammar;
    private ModuleCoordinator? coordinator;
    private SeedModule? seedModule;
    private SeedService? seedService;
    private string? toolkitDirectory;

    public override void OnInitializeMelon()
    {
        CapabilityRegistry capabilities = new();
        SeedService seeds = new();
        GameModel model = new(capabilities);
        ModuleRegistry modules = new();
        SeedModule seedModule = new(seeds);
        modules.Register(seedModule);

        string toolkitDirectory = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "TowerDominionToolkit");
        string configurationPath = Path.Combine(toolkitDirectory, "modules.json");
        ModuleConfiguration configuration;

        try
        {
            configuration = new ModuleConfigurationStore(configurationPath).Load();
        }
        catch (Exception exception)
        {
            MelonLogger.Error($"[Toolkit] Cannot load module configuration: {exception}");
            return;
        }

        this.seedModule = seedModule;
        seedService = seeds;
        this.toolkitDirectory = toolkitDirectory;
        coordinator = new ModuleCoordinator(modules, configuration, model);
        coordinator.ModuleStatusChanged += OnModuleStatusChanged;
        coordinator.Start();

        MelonLogger.Msg("[Toolkit] Initialized. Seed commands are available through UserData/TowerDominionToolkit/seed-command.txt.");
    }

    private void OnModuleStatusChanged(object? sender, ModuleStartupResult result)
    {
        if (result.Status != ModuleStatus.Active || result.Id != seedModule?.Id
            || seedService is null || toolkitDirectory is null)
        {
            return;
        }

        SeedBridge = new SeedRuntimeBridge(seedService);
        Seeds = seedModule;
        EntityCatalog catalog = new();
        ChallengeCatalog = catalog;
        catalogAdapter = new GameCatalogAdapter(catalog);
        try
        {
            FrontierSelections frontierSelections = new(Path.Combine(toolkitDirectory, "frontier-selection.json"));
            SavedFrontiers = frontierSelections;
        }
        catch (Exception exception)
        {
            MelonLogger.Error($"[Toolkit] Frontier selection configuration failed: {exception}");
        }
        try
        {
            SavedBans = new BanSelections(Path.Combine(toolkitDirectory, "bans.json"), persist: false);
        }
        catch (Exception exception) { MelonLogger.Error($"[Toolkit] Ban configuration failed: {exception}"); }
        try
        {
            StartingTileSets = new StartingTilePresets(Path.Combine(toolkitDirectory, "StartingTiles"));
        }
        catch (Exception exception) { MelonLogger.Error($"[Toolkit] Starting tile configuration failed: {exception}"); }
        ChallengeRegistry? challengeRegistry = null;
        try
        {
            challengeRegistry = new ChallengeRegistry();
            challengeRegistry.Register(new CheapestTowerOnlyRule());
            challengeRegistry.Register(new LowTechRule());
            challengeRegistry.Register(new ForcedDeploymentRule());
            challengeRegistry.Register(new ChainOfCommandRule());
            challengeRegistry.Register(new LeftTurnOnlyRule());
            challengeRegistry.Register(new EightBallRule());
            challengeRegistry.Register(new FrozenLogisticsRule());
            SavedChallenges = new ChallengeSelections(Path.Combine(toolkitDirectory, "challenge-selection.json"), challengeRegistry);
            ChallengeRules = new ChallengeEnforcement(challengeRegistry, SavedChallenges);
            challengeHud = new ChallengeHudOverlay(challengeRegistry);
            challengePreRun = new ChallengePreRunOverlay(challengeRegistry);
            chainFrontier = new ChainOfCommandFrontierOverlay();
        }
        catch (Exception exception)
        {
            SavedChallenges = null;
            ChallengeRules = null;
            MelonLogger.Error($"[Toolkit] Challenge configuration failed: {exception}");
        }
        // Compose canonical commands against the same objects used by game patches.
        if (catalogAdapter is not null && SavedBans is not null && SavedFrontiers is not null
            && challengeRegistry is not null && SavedChallenges is not null
            && StartingTileSets is not null)
        {
            try
            {
                canonicalGrammar = GameCommandComposition.Create(catalogAdapter, SavedBans,
                    SavedFrontiers, challengeRegistry, SavedChallenges, StartingTileSets,
                    seedService, () => $"Toolkit {ToolkitBuildInfo.Version}; seed module active.",
                    Path.Combine(toolkitDirectory, "Presets"));
            }
            catch (Exception exception)
            {
                canonicalGrammar = null;
                MelonLogger.Error($"[Toolkit] Canonical command initialization failed: {exception}");
            }
        }
        if (canonicalGrammar is not null)
        {
            CanonicalCommandExecutor executor = new(canonicalGrammar);
            commandInbox = new SeedCommandInbox(toolkitDirectory, executor);
            consoleOverlay = new ToolkitConsoleOverlay(executor, canonicalGrammar);
        }
        else MelonLogger.Error("[Toolkit] Canonical grammar unavailable; command transports disabled.");
        MelonLogger.Msg("[Toolkit] Seed module activated.");
    }

    public override void OnDeinitializeMelon()
    {
        if (coordinator is not null)
        {
            coordinator.ModuleStatusChanged -= OnModuleStatusChanged;
            coordinator.Dispose();
            coordinator = null;
        }

        SeedBridge = null;
        Seeds = null;
        SavedFrontiers = null;
        SavedBans = null;
        ChallengeCatalog = null;
        StartingTileSets = null;
        SavedChallenges?.EndRun();
        SavedChallenges = null;
        ChallengeRules = null;
        challengeHud = null;
        challengePreRun = null;
        chainFrontier = null;
        consoleOverlay?.Dispose();
        consoleOverlay = null;
        commandInbox = null;
        catalogAdapter = null;
        canonicalGrammar = null;
    }

    public override void OnGUI()
    {
        chainFrontier?.Draw(consoleOverlay?.IsVisible ?? false);
        challengeHud?.Draw();
        challengePreRun?.Draw();
        consoleOverlay?.Draw();
    }

    public override void OnUpdate()
    {
        if (SeedGenerationPatch.PendingVerificationService is { } service && SeedBridge is { } bridge)
        {
            SeedGenerationPatch.ClearPendingVerification();
            try
            {
                bridge.ObserveEffectiveSeed(service.GetEncodedSeed());
                MelonLogger.Msg("[Toolkit] Independently observed effective seed after generation.");
            }
            catch (Exception exception)
            {
                MelonLogger.Warning($"[Toolkit] Seed verification read failed: {exception.Message}");
            }
        }

        FrontierStartGuard.Update();
        catalogAdapter?.RefreshWhenReady();
#if DEBUG
        ChallengeHudProbe.Update();
#endif
        commandInbox?.Poll();
        consoleOverlay?.Update();
    }
}
