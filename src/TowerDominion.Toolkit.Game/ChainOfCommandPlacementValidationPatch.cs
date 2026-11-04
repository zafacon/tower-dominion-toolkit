using HarmonyLib;
using Il2CppNvizzio.Core.Singleton;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using MelonLoader;
using TowerDominion.Toolkit.Modules;
using Cell = TowerDominion.Toolkit.Modules.BuildingConnectivity.Cell;

namespace TowerDominion.Toolkit.Game;

/// <summary>Enforce orthogonal connectivity using native entity-owned tile footprints.
/// The native validator still decides terrain, occupancy and other placement rules.</summary>
[HarmonyPatch(typeof(BuildingEntity), nameof(BuildingEntity.ValidatePlacement))]
internal static class ChainOfCommandPlacementValidationPatch
{
    private static int missingFootprintReports;

    private static void Postfix(BuildingEntity __instance, ref bool __result)
    {
        if (!__result) return;
        var selections = ToolkitMod.SavedChallenges;
        if (selections?.RunActive != true ||
            !selections.Active.Contains(ChainOfCommandRule.Id, StringComparer.OrdinalIgnoreCase)) return;
        try
        {
            var manager = MonoSingleton<EntityManager>.Instance;
            var hq = manager?.GetHQEntity();
            if (manager == null || hq == null || __instance.Pointer == hq.Pointer) return;
            var candidate = Cells(__instance);
            if (candidate.Count == 0)
            {
                // Previews can have no TileBasements. Use the game's own rotated
                // root bounds rather than treating a multi-tile building as 1x1.
                var start = __instance.GetRootStartTilePos();
                var end = __instance.GetRootEndTilePos();
                var size = __instance.BuildingSize;
                int expected = (int)Math.Round(size.x) * (int)Math.Round(size.y);
                if (start != null && end != null && expected > 0 && expected <= 64)
                {
                    int minX = Math.Min(start.X, end.X), maxX = Math.Max(start.X, end.X);
                    int minY = Math.Min(start.Y, end.Y), maxY = Math.Max(start.Y, end.Y);
                    if ((maxX - minX + 1) * (maxY - minY + 1) == expected)
                        for (int x = minX; x <= maxX; x++)
                            for (int y = minY; y <= maxY; y++)
                                candidate.Add(new Cell(x, y));
                }
                if (candidate.Count == 0 && expected == 1)
                {
                    var root = __instance.GetRootGlobalizedTilePos();
                    if (root != null) candidate.Add(new Cell(root.X, root.Y));
                }
                if (candidate.Count == 0)
                {
                    if (++missingFootprintReports <= 12)
                        MelonLogger.Warning($"[ChainOfCommand] Unresolved candidate footprint, rejecting: {__instance.BuildingType} size={size} start={start?.X},{start?.Y} end={end?.X},{end?.Y}");
                    __result = false;
                    return;
                }
            }
            var headquarters = Cells(hq);
            var buildings = new List<IReadOnlyCollection<Cell>>();
            foreach (var building in manager.Buildings)
            {
                if (building == null || building.Pointer == hq.Pointer ||
                    building.Pointer == __instance.Pointer || building.IsBuildingPreview) continue;
                var footprint = Cells(building);
                if (footprint.Count > 0) buildings.Add(footprint);
            }
            bool allowed = BuildingConnectivity.CanPlace(candidate, headquarters, buildings);
            if (!allowed)
            {
                __result = false;
                MelonLogger.Msg($"[ChainOfCommand] Placement unavailable: {__instance.BuildingType} at {string.Join(",", candidate)}");
            }
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[ChainOfCommand] Placement validation failed: {exception.Message}");
        }
    }

    private static List<Cell> Cells(BuildingEntity building)
    {
        var result = new List<Cell>();
        var tiles = building.TileBasements;
        if (tiles == null) return result;
        foreach (var tile in tiles)
        {
            var position = tile?.GlobalizedTilePos;
            if (position != null) result.Add(new Cell(position.X, position.Y));
        }
        return result;
    }
}
