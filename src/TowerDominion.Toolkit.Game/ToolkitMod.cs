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

    internal static StartingTilePresets? StartingTileSets { get; private set; }

    private SeedCommandInbox? commandInbox;
    private ToolkitConsoleOverlay? consoleOverlay;
    private GameCatalogAdapter? catalogAdapter;
    private ModuleCoordinator? coordinator;
    private ModuleRegistry? coordinatorModules;
    private CapabilityRegistry? coordinatorCapabilities;
    private SeedModule? seedModule;
    private SeedService? seedService;
    private string? toolkitDirectory;

    public override void OnInitializeMelon()
    {
        CapabilityRegistry capabilities = new();
        SeedService seeds = new();
        GameModel model = new(capabilities);
        ModuleRegistry modules = new();
        coordinatorModules = modules;
        coordinatorCapabilities = capabilities;
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
        catalogAdapter = new GameCatalogAdapter(catalog);
        CommandRegistry commands = new();
        VersionCommands versionCommands = new(commands, coordinatorModules!, coordinatorCapabilities!);
        commands.Register(new ToolkitCommand("version", "version", "Show build identity and registered features.", versionCommands.Execute));
        SeedCommands seedCommands = new(seedModule, seedService);
        commands.Register(new ToolkitCommand("seed", "seed set <value> | seed clear | seed status", "Control the next run seed.", seedCommands.Execute));
        CatalogCommands catalogCommands = new(catalog, () => catalogAdapter?.IsReady == true);

        try
        {
            FrontierSelections frontierSelections = new(Path.Combine(toolkitDirectory, "frontier-selection.json"));
            SavedFrontiers = frontierSelections;
            FrontierCommands frontierCommands = new(catalog, frontierSelections, () => catalogAdapter?.IsReady == true);
            commands.Register(new ToolkitCommand("select", "select frontier <names|all>", "Persistently request Frontier modifiers.", frontierCommands.Execute));
            commands.Register(new ToolkitCommand("unselect", "unselect frontier <names|all>", "Remove persistent Frontier requests.", frontierCommands.Execute));
            commands.Register(new ToolkitCommand("list", "list <foes|waves|blueprints|doctrines|frontiers|selected-frontiers>", "List game entities, selected Frontiers, or bans.", input => (input.Trim().StartsWith("list starting-tile", StringComparison.OrdinalIgnoreCase) || (input.Trim().Equals("list relative tiles", StringComparison.OrdinalIgnoreCase) || input.Trim().Equals("list starting tiles", StringComparison.OrdinalIgnoreCase))) && StartingTileSets != null ? new StartingTileCommands(StartingTileSets).Execute(input) : input.Trim().Equals("list selected-frontiers", StringComparison.OrdinalIgnoreCase) ? frontierCommands.ListSelected(input) : input.Trim().StartsWith("list banlist", StringComparison.OrdinalIgnoreCase) && SavedBans != null ? new NamedBanlists(Path.Combine(toolkitDirectory, "Banlists"), SavedBans, catalog).Execute(input) : input.Trim().Equals("list banned", StringComparison.OrdinalIgnoreCase) ? SavedBans == null ? "Ban state unavailable." : new BanCommands(catalog, SavedBans, () => true).List(input) : catalogCommands.Execute(input)));
        }
        catch (Exception exception)
        {
            MelonLogger.Error($"[Toolkit] Frontier selection configuration failed: {exception}");
            commands.Register(new ToolkitCommand("list", "list <foes|waves|blueprints|doctrines|frontiers>", "List game entities.", catalogCommands.Execute));
        }
        try
        {
            SavedBans = new BanSelections(Path.Combine(toolkitDirectory, "bans.json"), persist: false);
            BanCommands banCommands = new(catalog, SavedBans, () => catalogAdapter?.IsReady == true);
            commands.Register(new ToolkitCommand("ban", "ban <foes|waves|blueprints|doctrines> <names|all>", "Ban game entities for this session.", banCommands.Execute));
            commands.Register(new ToolkitCommand("unban", "unban <foes|waves|blueprints|doctrines> <names|all>", "Remove active bans.", banCommands.Execute));
            NamedBanlists named = new(Path.Combine(toolkitDirectory, "Banlists"), SavedBans, catalog);
            commands.Register(new ToolkitCommand("save", "save banned as <name>", "Save active bans as a named preset.", input => input.Trim().StartsWith("save starting tiles", StringComparison.OrdinalIgnoreCase) && StartingTileSets != null ? new StartingTileCommands(StartingTileSets).Execute(input) : named.Execute(input)));
            commands.Register(new ToolkitCommand("load", "load banlist <name>", "Replace active bans with a saved preset.", input => input.Trim().StartsWith("load starting tiles", StringComparison.OrdinalIgnoreCase) && StartingTileSets != null ? new StartingTileCommands(StartingTileSets).Execute(input) : named.Execute(input)));
            commands.Register(new ToolkitCommand("unload", "unload banlist <name>", "Remove saved entries from active bans.", input => input.Trim().StartsWith("unload starting tiles", StringComparison.OrdinalIgnoreCase) && StartingTileSets != null ? new StartingTileCommands(StartingTileSets).Execute(input) : named.Execute(input)));
            commands.Register(new ToolkitCommand("delete", "delete banlist <name>", "Delete a named banlist.", input => input.Trim().StartsWith("delete starting-tile-preset", StringComparison.OrdinalIgnoreCase) && StartingTileSets != null ? new StartingTileCommands(StartingTileSets).Execute(input) : named.Execute(input)));
            commands.Register(new ToolkitCommand("banned", "banned", "List active bans.", _ => banCommands.List("list banned")));
        }
        catch (Exception exception) { MelonLogger.Error($"[Toolkit] Ban configuration failed: {exception}"); }
        try
        {
            StartingTileSets = new StartingTilePresets(Path.Combine(toolkitDirectory, "StartingTiles"));
            StartingTileCommands tileCommands = new(StartingTileSets);
            commands.Register(new ToolkitCommand("activate", "activate tileset <name>", "Persistently activate a saved starting tileset.", tileCommands.Execute));
            commands.Register(new ToolkitCommand("deactivate", "deactivate tileset", "Clear the persistent starting tileset.", tileCommands.Execute));
            commands.Register(new ToolkitCommand("set", "set starting tiles N:S,E:X,S:S,W:S | set relative tiles D3 L.C,T.A,X", "Configure HQ-adjacent tiles for future runs.", tileCommands.Execute));
            commands.Register(new ToolkitCommand("clear", "clear relative tiles D1|D2|D3|D4", "Remove difficulty-specific starting tiles.", tileCommands.Execute));
            // The existing save/load/unload/delete/list handlers also accept starting-tile operations.
            // Their dispatch is integrated below without changing existing banlist behavior.
        }
        catch (Exception exception) { MelonLogger.Error($"[Toolkit] Starting tile configuration failed: {exception}"); }
        ToolkitCommandHelp.Register(commands);
        commands.Register(new ToolkitCommand("frontier-diagnostics", "frontier-diagnostics <on|off|status>", "Trace game-owned Frontier selection state without changing it.", input =>
        {
            string option = input.Substring("frontier-diagnostics".Length).Trim();
            if (option.Equals("on", StringComparison.OrdinalIgnoreCase)) FrontierDiagnostics.Enabled = true;
            else if (option.Equals("off", StringComparison.OrdinalIgnoreCase)) FrontierDiagnostics.Enabled = false;
            else if (!option.Equals("status", StringComparison.OrdinalIgnoreCase)) return "Usage: frontier-diagnostics <on|off|status>";
            return $"Frontier diagnostics: {(FrontierDiagnostics.Enabled ? "on" : "off")}.";
        }));
        commandInbox = new SeedCommandInbox(toolkitDirectory, commands);
        consoleOverlay = new ToolkitConsoleOverlay(commands, catalog, StartingTileSets);
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

        coordinatorModules = null;
        coordinatorCapabilities = null;
        SeedBridge = null;
        Seeds = null;
        SavedFrontiers = null;
        SavedBans = null;
        StartingTileSets = null;
        consoleOverlay?.Dispose();
        consoleOverlay = null;
        commandInbox = null;
        catalogAdapter = null;
    }

    public override void OnGUI() => consoleOverlay?.Draw();

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

        catalogAdapter?.RefreshWhenReady();
        commandInbox?.Poll();
        consoleOverlay?.Update();
    }
}
