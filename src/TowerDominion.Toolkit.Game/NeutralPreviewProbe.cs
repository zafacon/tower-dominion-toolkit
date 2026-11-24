using HarmonyLib;
using Il2CppNvizzio.Game.Services;
using Il2CppNvizzio.Game.Services.Utils;
using Il2CppNvizzio.Game.GamePlay.Zones;
using Il2CppNvizzio.Game.GamePlay.Contexts;
using Il2CppNvizzio.Game.GamePlay.Zones.Coordinates;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Buildings;
using Il2CppNvizzio.Game.GamePlay.Zones.References;
using Il2CppNvizzio.Core.Singleton;
using MelonLoader;
using TowerDominion.Toolkit.Rng;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

internal static class NeutralPreviewProbe
{
    [ThreadStatic] internal static int SpawnDepth;
    internal static Prediction? Pending;
    internal static Zone? CurrentPreview;
    internal static BuildingEntity? VisualPreview;
    internal static Vector3? VisualPreviewWorldPosition;
    internal static Prediction? PreCommitPrediction;
    internal static int PreCommitSeed;
    internal sealed record Prediction(int Seed, nint Basement, int X, int Y, string BuildingId, int OrientationRoll);

    internal static int ZoneSeed
    {
        get
        {
            try { return Services.RandomService.CurrentSeeds[RandomType.Zone]; }
            catch { return 0; }
        }
        set => Services.RandomService.CurrentSeeds[RandomType.Zone] = value;
    }

    internal static void Log(string stage, string detail = "")
    {
        try { MelonLogger.Msg($"[NeutralPreviewProbe] {stage} seed={ZoneSeed} {detail}"); }
        catch (Exception ex) { MelonLogger.Warning($"[NeutralPreviewProbe] {stage}: {ex.Message}"); }
    }

    internal static GlobalizedTilePos LocalPreviewToGlobal(Zone zone, int localX, int localY)
    {
        Vector3 world = zone.transform.TransformPoint(new Vector3(localX, 0f, localY));
        return new GlobalizedTilePos(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));
    }

    internal static bool TryGetCanonicalTileHeight(
        Zone zone, ZoneBasement basement, int localX, int localY, out int height)
    {
        // TryGetTileHeight applies Zone.Angle internally. Counter-rotate the query
        // so its native rotation resolves the canonical, unrotated zone cell Y.
        Vector2Int query = ZoneUtils.Vecton2IntRotation(localX, localY, -zone.Angle);
        return zone.TryGetTileHeight(
            new NormalizedTilePos(query.x, query.y, basement.ZonePos), out height);
    }

    internal static bool IsIntrinsicLocalCellValid(ZoneBasement basement, int localX, int localY)
    {
        var zone = CurrentPreview;
        if (zone == null || basement?.ZonePos == null)
            return false;

        int height;
        if (!TryGetCanonicalTileHeight(zone, basement, localX, localY, out height) || height <= 0)
            return false;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = localX + dx;
                int ny = localY + dy;
                if (nx < -2 || nx > 2 || ny < -2 || ny > 2)
                    continue;

                int neighborHeight;
                if (TryGetCanonicalTileHeight(zone, basement, nx, ny, out neighborHeight) &&
                    neighborHeight > 0)
                    return true;
            }

        return false;
    }

    internal static string Position(Transform? transform)
    {
        if (transform == null) return "null";
        Vector3 p = transform.position;
        return $"({p.x:F2},{p.y:F2},{p.z:F2})";
    }

    internal static string LocalPosition(Transform? transform)
    {
        if (transform == null) return "null";
        Vector3 p = transform.localPosition;
        return $"({p.x:F2},{p.y:F2},{p.z:F2})";
    }

    internal static void DestroyVisualPreview()
    {
        try
        {
            if (VisualPreview != null)
                UnityEngine.Object.Destroy(VisualPreview.gameObject);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[NeutralPreviewProbe] visual cleanup failed: {ex.Message}");
        }
        VisualPreview = null;
        VisualPreviewWorldPosition = null;
    }

    internal static void EnforceVisualPreviewPosition()
    {
        // Diagnostic build: leave the known-visible local preview untouched.
    }

    internal static void ShowVisualPreview(ZoneBasement basement)
    {
        DestroyVisualPreview();
        if (ToolkitMod.SavedGameplayRules?.IsEnabled("neutral-tile-preview") != true)
            return;

        var p = Pending;
        var zone = CurrentPreview;
        if (p == null || zone == null || p.Basement != basement.Pointer) return;

        try
        {
            if (CorrectiveNeutralPlacement.PositionX is not int localX ||
                CorrectiveNeutralPlacement.PositionY is not int localY)
                throw new InvalidOperationException("Corrective neutral canonical local cell is unavailable");

            int height;
            if (!TryGetCanonicalTileHeight(zone, basement, localX, localY, out height))
                throw new InvalidOperationException("Predicted canonical tile has no preview height");

            var data = BuildingData.ResourceLoadNeutral(p.BuildingId);
            if (data == null) throw new InvalidOperationException($"BuildingData missing for {p.BuildingId}");

            var entity = data.Instantiate($"ToolkitNeutralPreview_{p.BuildingId}", zone.transform, false, null);
            if (entity == null) throw new InvalidOperationException($"Could not instantiate preview {p.BuildingId}");

            entity.IsBuildingPreview = true;
            Vector3 desiredLocal = new(localX, 0.25f * (height + 1), localY);
            entity.transform.localPosition = desiredLocal;
            VisualPreviewWorldPosition = null;
            entity.SetRotation(p.OrientationRoll * 90f);

            VisualPreview = entity;
            Log("visual", $"id={p.BuildingId} projectedGlobal=({p.X},{p.Y}) canonicalY=({localX},{localY}) previewHeight={height} initialLocal=({desiredLocal.x:F2},{desiredLocal.y:F2},{desiredLocal.z:F2}) world={Position(entity.transform)} local={LocalPosition(entity.transform)} preview={entity.IsBuildingPreview}");
        }
        catch (Exception ex)
        {
            DestroyVisualPreview();
            MelonLogger.Warning($"[NeutralPreviewProbe] visual failed: {ex}");
        }
    }


    private static Exception? Finalizer(Exception? __exception)
    {
        if (__exception is not null)
        {
            if (CorrectiveNeutralPlacement.NativeSpawnDepth > 0)
                CorrectiveNeutralPlacement.NativeSpawnDepth--;
            if (NeutralPreviewProbe.SpawnDepth > 0)
                NeutralPreviewProbe.SpawnDepth--;
        }

        return __exception;
    }
}

[HarmonyPatch(typeof(BuildingEntity), nameof(BuildingEntity.ApplyVisualOffset))]
internal static class NeutralToolkitPreviewVisualOffsetPatch
{
    private static bool Prefix(BuildingEntity __instance)
    {
        return NeutralPreviewProbe.VisualPreview == null ||
               __instance.Pointer != NeutralPreviewProbe.VisualPreview.Pointer;
    }
}

[HarmonyPatch(typeof(PlaceTheNewZoneContext), nameof(PlaceTheNewZoneContext.RerollZone))]
internal static class CorrectiveNeutralRerollBoundaryPatch
{
    private static void Prefix()
    {
        if (CorrectiveNeutralPlacement.Opportunity is not null)
        {
            NeutralPositionDecision? current = CorrectiveNeutralPlacement.Opportunity.CurrentPosition;
            Zone? zone = NeutralPreviewProbe.CurrentPreview;
            NeutralPreviewProbe.Log(
                "reroll-boundary",
                $"zone={zone?.ZoneId?.name} angle={zone?.Angle:F0} currentLocal=({current?.XOffset},{current?.YOffset})");
            CorrectiveNeutralPlacement.PositionNeedsReroll = true;
        }
    }
}

[HarmonyPatch(typeof(ZoneBasement), nameof(ZoneBasement.AddZonePreview))]
internal static class NeutralPreviewAttachProbe
{
    private static void Postfix(ZoneBasement __instance, Zone preview)
    {
        NeutralPreviewProbe.CurrentPreview = preview;
        NeutralPreviewProbe.Log("preview", $"basement={__instance.Pointer} zone={preview?.ZoneId?.name} angle={preview?.Angle:F0} hostZonePos=({__instance.ZonePos?.X},{__instance.ZonePos?.Y}) previewPos={NeutralPreviewProbe.Position(preview?.transform)}");
        try
        {
            CorrectiveNeutralPlacement.BeginOrRefresh(__instance);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[Toolkit RNG] Corrective neutral preview unavailable: {ex}");
            CorrectiveNeutralPlacement.Complete();
            NeutralPreviewProbe.Pending = null;
        }
        NeutralPreviewProbe.ShowVisualPreview(__instance);
    }
}

[HarmonyPatch(typeof(PlaceTheNewZoneContext), nameof(PlaceTheNewZoneContext.OnZoneBasementLeftClick))]
internal static class NeutralPreviewCommitProbe
{
    private static void Prefix(ZoneBasement zoneBasement)
    {
        NeutralPreviewProbe.DestroyVisualPreview();
        try
        {
            CorrectiveNeutralPlacement.PrepareCommit();
            CorrectiveNeutralPlacement.LatchCommitWorldCell();
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Toolkit RNG] Corrective neutral commit preparation failed: {ex}");
            CorrectiveNeutralPlacement.Complete();
            NeutralPreviewProbe.Pending = null;
        }
        NeutralPreviewProbe.PreCommitPrediction = NeutralPreviewProbe.Pending;
        NeutralPreviewProbe.PreCommitSeed = NeutralPreviewProbe.ZoneSeed;
        var p = NeutralPreviewProbe.Pending;
        bool sameBasement = p != null && zoneBasement != null && p.Basement == zoneBasement.Pointer;
        bool corrective = CorrectiveNeutralPlacement.IsActive;
        bool sameSeed = corrective || (p != null && p.Seed == NeutralPreviewProbe.ZoneSeed);
        NeutralPreviewProbe.Log("commit", $"basement={zoneBasement?.Pointer} zonePos=({zoneBasement?.ZonePos?.X},{zoneBasement?.ZonePos?.Y}) savedPrediction={p != null} sameBasement={sameBasement} sameSeed={sameSeed} corrective={corrective}");
        if (!sameBasement || !sameSeed) NeutralPreviewProbe.Pending = null;
    }
}

[HarmonyPatch(typeof(ZoneBasement), nameof(ZoneBasement.RegisterZone))]
internal static class NeutralPreviewPostRegisterProbe
{
    private static void Postfix(ZoneBasement __instance)
    {
        if (CorrectiveNeutralPlacement.IsActive)
            CorrectiveNeutralPlacement.CaptureCommittedTile(__instance);

        var p = NeutralPreviewProbe.PreCommitPrediction;
        if (p == null || p.Basement != __instance.Pointer) return;
        try
        {
            GlobalizedTilePos target = CorrectiveNeutralPlacement.CommitWorldCell ?? new GlobalizedTilePos(p.X, p.Y);
            var manager = MonoSingleton<ZoneManager>.Instance;
            var tile = manager?.GetTileBasementByGlobalizedTilePos(target);
            NeutralPreviewProbe.Log("post-register", $"target=({target.X},{target.Y}) tile={(tile == null ? "null" : tile.Pointer.ToString())} free={tile?.IsFree} height={tile?.TileHeight} world={NeutralPreviewProbe.Position(tile?.Transform)} seedSame={NeutralPreviewProbe.ZoneSeed == NeutralPreviewProbe.PreCommitSeed}");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[NeutralPreviewProbe] post-register failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.IsNeutralBuildingToPlace))]
internal static class NeutralPreviewDiscoveryProbe
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix(ZoneManager __instance) => NeutralPreviewProbe.Log("discovery-before", $"odds={__instance.currentNeutralOdds}");
    private static void Postfix(ZoneManager __instance, bool __result) => NeutralPreviewProbe.Log("discovery-after", $"result={__result} odds={__instance.currentNeutralOdds}");
}

[HarmonyPatch(typeof(EntityFactory), nameof(EntityFactory.InstantiateNeutralTower))]
internal static class NeutralPreviewSpawnProbe
{
    private static void Postfix(string buildingId, BuildingEntity __result)
    {
        var p = NeutralPreviewProbe.Pending;
        string verdict = p == null ? "no-prediction" : $"idMatch={p.BuildingId == buildingId}";
        NeutralPreviewProbe.Log("spawn", $"id={buildingId} {verdict}");
    }
}

[HarmonyPatch(typeof(PlaceTheNewZoneContext._SpawnNeutralBuilding_d__18), "MoveNext")]
internal static class NeutralPreviewCoroutineProbe
{
    private static void Prefix()
    {
        NeutralPreviewProbe.SpawnDepth++;
        CorrectiveNeutralPlacement.NativeSpawnDepth++;
    }

    private static void Postfix(PlaceTheNewZoneContext._SpawnNeutralBuilding_d__18 __instance, bool __result)
    {
        try
        {
            if (__result)
            {
                if (CorrectiveNeutralPlacement.IsActive)
                    __instance._tileBasement_5__2 = CorrectiveNeutralPlacement.ResolveCommittedTile();
                return;
            }

            var tile = __instance._tileBasement_5__2;
            var building = __instance._building_5__3;
            var p = NeutralPreviewProbe.Pending;
            if (p == null)
            {
                NeutralPreviewProbe.Log("verify", "no-prediction");
                return;
            }

            GlobalizedTilePos target = CorrectiveNeutralPlacement.CommitWorldCell ?? new GlobalizedTilePos(p.X, p.Y);
            bool tileMatch = tile != null && tile.GlobalizedTilePos.X == target.X && tile.GlobalizedTilePos.Y == target.Y;
            bool positionMatch = tile != null && building != null &&
                Vector3.Distance(tile.Transform.position, building.transform.position) < 0.01f;
            NeutralPreviewProbe.Log(
                "verify",
                $"target=({target.X},{target.Y}) actual=({tile?.GlobalizedTilePos?.X},{tile?.GlobalizedTilePos?.Y}) tileMatch={tileMatch} positionMatch={positionMatch} final={NeutralPreviewProbe.Position(building?.transform)}");
            NeutralPreviewProbe.Pending = null;
            CorrectiveNeutralPlacement.Complete();
        }
        finally
        {
            CorrectiveNeutralPlacement.NativeSpawnDepth--;
            NeutralPreviewProbe.SpawnDepth--;
        }
    }
}
