using HarmonyLib;
using Il2CppNvizzio.Core.Singleton;
using Il2CppNvizzio.Game.GamePlay.Contexts;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using Il2CppNvizzio.Game.GamePlay.Zones;
using MelonLoader;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>
/// Collect native placement footprints before enforcing Chain of Command.
/// A clicked tile alone is insufficient to validate multi-tile buildings.
/// </summary>
[HarmonyPatch(typeof(PlaceTheNewBuildingContext), nameof(PlaceTheNewBuildingContext.OnTileBasementLeftClick))]
internal static class ChainOfCommandPlacementProbe
{
    private static int observations;

    private static void Prefix(TileBasement tileBasement)
    {
        var selections = ToolkitMod.SavedChallenges;
        if (selections?.RunActive != true ||
            !selections.Active.Contains(ChainOfCommandRule.Id, StringComparer.OrdinalIgnoreCase)) return;
        try
        {
            var position = tileBasement?.GlobalizedTilePos;
            var manager = MonoSingleton<EntityManager>.Instance;
            if (position == null || manager == null) return;
            var hq = manager.GetHQEntity();
            var hqRoot = hq?.GetRootGlobalizedTilePos();
            int count = 0;
            var examples = new List<string>();
            foreach (var building in manager.Buildings)
            {
                if (building == null || building.IsBuildingPreview) continue;
                count++;
                if (examples.Count >= 12) continue;
                var root = building.GetRootGlobalizedTilePos();
                var tiles = building.TileBasements;
                var cells = new List<string>();
                if (tiles != null)
                    foreach (var tile in tiles)
                    {
                        var cell = tile?.GlobalizedTilePos;
                        if (cell != null)
                        {
                            var occupant = tile!.Entity;
                            bool matches = occupant != null && occupant.Pointer == building.Pointer;
                            cells.Add($"({cell.X},{cell.Y}):{(matches ? "owned" : occupant == null ? "empty" : "other")}");
                        }
                    }
                examples.Add($"{building.BuildingType}@({root?.X},{root?.Y}) tiles=[{string.Join(",", cells)}]");
            }
            if (++observations <= 20)
                MelonLogger.Msg($"[ChainOfCommand probe] click=({position.X},{position.Y}) HQ=({hqRoot?.X},{hqRoot?.Y}) existing={count} {string.Join("; ", examples)}");
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[ChainOfCommand probe] {exception.Message}");
        }
    }
}
