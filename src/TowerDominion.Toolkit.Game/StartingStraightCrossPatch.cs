using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.Contexts;
using Il2CppNvizzio.Game.GamePlay.Paths;
using Il2CppNvizzio.Game.GamePlay.Zones;
using TowerDominion.Toolkit.Modules;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

/// <summary>Only native starting-zone creation calls at HQ-relative entrance coordinates receive overrides.</summary>
internal static class StartingTileScope
{
    [ThreadStatic] internal static bool Active;
    [ThreadStatic] internal static int X;
    [ThreadStatic] internal static int Y;
    internal static IReadOnlyDictionary<StartPosition, StartingTile>? RelativeRun;
    internal static bool TryGet(int x, int y, out StartingTile tile)
    {
        tile = null!;
        StartPosition position;
        if (x == 0 && y == 2) position = StartPosition.N;
        else if (x == 2 && y == 0) position = StartPosition.E;
        else if (x == 0 && y == -2) position = StartPosition.S;
        else if (x == -2 && y == 0) position = StartPosition.W;
        else return false;
        if (RelativeRun != null) return RelativeRun.TryGetValue(position, out tile!);
        return ToolkitMod.StartingTileSets?.TryGet(x, y, out tile!) == true;
    }
}

// Read-only diagnostic: capture the order of native HQ entrance creation.
// This does not alter the exit list, tile shapes, or the game's RNG.
[HarmonyPatch(typeof(MainLoopContext), nameof(MainLoopContext.CreateFirstZonePerExitPoint))]
internal static class HqEntranceTraceStartPatch
{
    private static void Prefix()
    {
        StartingTileScope.RelativeRun = null;
        HqEntranceTrace.Sequence = 0;
        HqEntranceTrace.ExitQueries = 0;
        HqEntranceTrace.FirstShapeChecked = false;
        HqEntranceTrace.CreationStarted = true;
        MelonLoader.MelonLogger.Msg("[HQEntrances] Beginning native entrance creation");
    }
}

internal static class HqEntranceTrace
{
    internal static int Sequence;
    internal static bool CreationStarted;
    internal static int ExitQueries;
    internal static bool FirstShapeChecked;
}

// Diagnostic only: inspect the native path-manager exit list while HQ zones are created.
[HarmonyPatch(typeof(PathsManager), nameof(PathsManager.GetExitPoints))]
internal static class HqExitPointsTracePatch
{
    private static void Postfix(Il2CppSystem.Collections.Generic.List<Il2CppNvizzio.Game.GamePlay.Zones.Coordinates.GlobalizedTilePos> __result)
    {
        if (!HqEntranceTrace.CreationStarted || ++HqEntranceTrace.ExitQueries > 8 || __result == null) return;
        var positions = new System.Collections.Generic.List<string>();
        foreach (var p in __result) positions.Add($"({p.X},{p.Y})");
        MelonLoader.MelonLogger.Msg($"[HQEntrances] GetExitPoints #{HqEntranceTrace.ExitQueries}: {string.Join(",", positions)}");
    }
}

[HarmonyPatch(typeof(MainLoopContext), nameof(MainLoopContext.CreateAndPlaceZone))]
internal static class HqEntranceTraceZonePatch
{
    private static void Prefix(Il2CppNvizzio.Game.GamePlay.Zones.Coordinates.GlobalizedTilePos exitPointPos)
    {
        int x = exitPointPos.X;
        int y = exitPointPos.Y;
        if (!((x == 0 && (y == 2 || y == -2)) || (y == 0 && (x == 2 || x == -2)))) return;
        string direction = y == 2 ? "N" : x == 2 ? "E" : y == -2 ? "S" : "W";
        MelonLoader.MelonLogger.Msg($"[HQEntrances] #{++HqEntranceTrace.Sequence} {direction} ({x},{y})");
    }
}

[HarmonyPatch(typeof(MainLoopContext._CreateAndPlaceZone_d__30), "MoveNext")]
internal static class StartingTileScopePatch
{
    private static void Prefix(MainLoopContext._CreateAndPlaceZone_d__30 __instance)
    {
        StartingTileScope.Active = true;
        StartingTileScope.X = __instance.exitPointPos.X;
        StartingTileScope.Y = __instance.exitPointPos.Y;
    }

    private static void Postfix() => StartingTileScope.Active = false;
    private static Exception? Finalizer(Exception? __exception)
    {
        StartingTileScope.Active = false;
        return __exception;
    }
}

[HarmonyPatch(typeof(ZoneFactory), nameof(ZoneFactory.InstantiateRandomZone),
    new[] { typeof(Transform), typeof(bool), typeof(Il2CppSystem.Collections.Generic.HashSet<ZoneShape>), typeof(Zone) })]
internal static class StartingBasicTilesPatch
{
    private static void Prefix(Il2CppSystem.Collections.Generic.HashSet<ZoneShape> excludeShapes)
    {
        if (!StartingTileScope.Active || excludeShapes == null) return;
        if (!HqEntranceTrace.FirstShapeChecked)
        {
            HqEntranceTrace.FirstShapeChecked = true;
            try
            {
                // Read-only probe at the exact shape-selection boundary.
                var manager = UnityEngine.Object.FindObjectOfType<PathsManager>();
                if (manager == null) MelonLoader.MelonLogger.Msg("[HQEntrances] BeforeFirstShape: PathsManager unavailable");
                else
                {
                    var exits = manager.GetExitPoints();
                    var positions = new System.Collections.Generic.List<string>();
                    foreach (var p in exits) positions.Add($"({p.X},{p.Y})");
                    MelonLoader.MelonLogger.Msg($"[HQEntrances] BeforeFirstShape: {string.Join(",", positions)}");
                    var active = new System.Collections.Generic.HashSet<StartPosition>();
                    foreach (var point in exits)
                    {
                        if (point.X == 0 && point.Y == 2) active.Add(StartPosition.N);
                        else if (point.X == 2 && point.Y == 0) active.Add(StartPosition.E);
                        else if (point.X == 0 && point.Y == -2) active.Add(StartPosition.S);
                        else if (point.X == -2 && point.Y == 0) active.Add(StartPosition.W);
                    }
                    if (ToolkitMod.StartingTileSets?.TryGetRelative(active.Count, active, out var mapping) == true)
                    {
                        StartingTileScope.RelativeRun = mapping;
                        MelonLoader.MelonLogger.Msg($"[StartingTiles] Relative D{active.Count}: {string.Join(",", mapping.Values.Select(StartingTilePresets.Format))}");
                    }
                }
            }
            catch (Exception ex) { MelonLoader.MelonLogger.Warning($"[HQEntrances] BeforeFirstShape failed: {ex.Message}"); }
        }
        if (!StartingTileScope.TryGet(StartingTileScope.X, StartingTileScope.Y, out var requested)) return;
        if (requested.Shape is not (StartShape.S or StartShape.X or StartShape.L or StartShape.T)) return;
        var wanted = requested.Shape switch
        {
            StartShape.S => ZoneShape.StraigthLine,
            StartShape.X => ZoneShape.X,
            StartShape.T => ZoneShape.T,
            _ => ZoneShape.L
        };
        foreach (var shape in new[] { ZoneShape.NoPath, ZoneShape.L, ZoneShape.StraigthLine, ZoneShape.T, ZoneShape.X })
            if (shape != wanted) excludeShapes.Add(shape);
    }
}

[HarmonyPatch(typeof(ZoneBasement), nameof(ZoneBasement.RegisterZone),
    new[] { typeof(Zone), typeof(bool), typeof(bool) })]
internal static class StartingBasicOrientationPatch
{
    private static void Prefix(Zone zone)
    {
        if (!StartingTileScope.Active || zone == null) return;
        int x = StartingTileScope.X;
        int y = StartingTileScope.Y;
        if ((x == 0 && (y == 2 || y == -2)) || (y == 0 && (x == 2 || x == -2)))
        {
            string direction = y == 2 ? "N" : x == 2 ? "E" : y == -2 ? "S" : "W";
            MelonLoader.MelonLogger.Msg($"[HQEntrances] RegisterZone {direction} ({x},{y}) shape={zone.ZoneShape}");
        }
        if (!StartingTileScope.TryGet(x, y, out var requested)) return;
        if (requested.Shape == StartShape.L && zone.ZoneShape != ZoneShape.L) return;
        if (requested.Shape == StartShape.T && zone.ZoneShape != ZoneShape.T) return;
        if (requested.Shape is not (StartShape.L or StartShape.T)) return;
        foreach (int angle in new[] { 0, 90, 180, 270 })
        {
            zone.SetRotation(angle);
            var paths = CardinalPaths.None;
            if (zone.IsPathToNorth()) paths |= CardinalPaths.N;
            if (zone.IsPathToEast()) paths |= CardinalPaths.E;
            if (zone.IsPathToSouth()) paths |= CardinalPaths.S;
            if (zone.IsPathToWest()) paths |= CardinalPaths.W;
            // The north/south visual offset is verified for L and bare T.
            // Visual testing showed T.C rendering as T.A on every face, so
            // invert only the lateral branch of oriented T at the native boundary.
            CardinalPaths desired = requested.Paths;
            if (requested.Shape == StartShape.T)
            {
                CardinalPaths towardHq = StartingTiles.HqFacing(requested.Position);
                CardinalPaths outward = StartingTiles.Rotate(towardHq, 180);
                if ((desired & outward) != 0)
                {
                    CardinalPaths clockwise = StartingTiles.TurnClockwise(towardHq);
                    CardinalPaths anticlockwise = StartingTiles.TurnAnticlockwise(towardHq);
                    desired = towardHq | outward |
                        ((desired & clockwise) != 0 ? anticlockwise : clockwise);
                }
            }
            CardinalPaths target = requested.Position is StartPosition.N or StartPosition.S
                ? StartingTiles.Rotate(desired, 180)
                : desired;
            if (paths != target) continue;
            MelonLoader.MelonLogger.Msg($"[StartingTiles] {StartingTilePresets.Format(requested)} rotation={angle} nativePaths={paths} requestedPaths={requested.Paths} diagnosticNS={target != requested.Paths}");
            return;
        }
        MelonLoader.MelonLogger.Warning($"[StartingTiles] Unable to orient {StartingTilePresets.Format(requested)}");
    }
}
