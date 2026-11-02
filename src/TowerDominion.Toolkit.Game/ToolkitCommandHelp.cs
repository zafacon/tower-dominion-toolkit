using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

internal static class ToolkitCommandHelp
{
    internal static void Register(CommandRegistry commands)
    {
        void Add(string verb, string usage, string summary, params string[] topics) =>
            commands.AddHelp(verb, usage, summary, topics);

        Add("list", "list <foes|waves|blueprints|doctrines|frontiers>", "List game entities.");
        Add("list", "list selected-frontiers", "Show selected Frontier modifiers.", "frontier");
        Add("select", "select frontier <name[,name...]|all>", "Persistently select Frontier modifiers.", "frontier");
        Add("unselect", "unselect frontier <name[,name...]|all>", "Remove persistent Frontier selections.", "frontier");
        if (commands.Commands.Any(x => x.Name == "ban"))
        {
            Add("list", "list banned", "Show session bans.", "banlists");
            Add("list", "list banlists", "Show saved banlists.", "banlists");
            Add("list", "list banlist <name>", "Inspect a saved banlist.", "banlists");
            Add("save", "save banned as <name>", "Save current bans.", "banlists");
            Add("load", "load banlist <name>", "Replace current bans.", "banlists");
            Add("unload", "unload banlist <name>", "Remove listed bans.", "banlists");
            Add("delete", "delete banlist <name>", "Delete a saved banlist.", "banlists");
        }
        if (commands.Commands.Any(x => x.Name == "set"))
        {
            Add("set", "set starting tiles <shape[,shape...]>", "Set relative tiles; number of shapes determines D1–D4.", "starting tiles");
            Add("set", "set relative tiles D1|D2|D3|D4 <shapes>", "Set relative tiles for an explicit difficulty.", "starting tiles");
            Add("clear", "clear relative tiles D1|D2|D3|D4", "Clear a difficulty's relative tiles.", "starting tiles");
            Add("clear", "clear starting tiles 1|2|3|4", "Alias for clearing relative tiles.", "starting tiles");
            Add("list", "list starting tiles", "Show current starting tiles.", "starting tiles");
            Add("list", "list relative tiles", "Show current relative tiles.", "starting tiles");
            Add("list", "list starting-tile-presets", "Show saved presets.", "starting tiles");
            Add("list", "list starting-tile-preset <name>", "Inspect a saved preset.", "starting tiles");
            Add("save", "save starting tiles as <name>", "Save starting tiles as a preset.", "starting tiles");
            Add("load", "load starting tiles <name>", "Load a starting-tile preset.", "starting tiles");
            Add("unload", "unload starting tiles", "Clear active starting tiles.", "starting tiles");
            Add("delete", "delete starting-tile-preset <name>", "Delete a saved preset.", "starting tiles");
            Add("activate", "activate tileset <name>", "Activate a saved tileset across restarts.", "starting tiles");
            Add("deactivate", "deactivate tileset", "Deactivate the persistent tileset.", "starting tiles");
        }
    }
}
