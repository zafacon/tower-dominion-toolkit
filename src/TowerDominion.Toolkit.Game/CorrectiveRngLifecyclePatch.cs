using HarmonyLib;
using Il2CppNvizzio.Core.SaveManager;
using Il2CppNvizzio.Game.Services;
using Il2CppNvizzio.Game.Services.Threats;
using Il2CppNvizzio.Game.Services.Utils;
using Il2CppNvizzio.Game.GamePlay.Zones;
using Il2CppNvizzio.Game.GamePlay.Contexts;
using Il2CppNvizzio.Game.Utils;
using MelonLoader;
using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Game;

/// <summary>
/// Observes the native run identity after StartNewGame has finalized ProgressSaveData.
/// Observation is deliberately non-attaching: it consumes no corrective RNG.
/// </summary>
[HarmonyPatch(typeof(GameRoundService), nameof(GameRoundService.StartNewGame))]
internal static class CorrectiveRngRunRootPatch
{
    private static void Postfix()
    {
        CorrectiveRngLifecycle? lifecycle = ToolkitMod.CorrectiveRng;
        if (lifecycle is null)
            return;

        try
        {
            string? encodedGameId = Services.RandomService?.GetEncodedSeed();
            if (string.IsNullOrWhiteSpace(encodedGameId))
            {
                MelonLogger.Warning("[Toolkit RNG] Native run started without an observable GAME ID; corrective RNG remains detached.");
                lifecycle.Clear();
                return;
            }

            int nativeSeed = CustomTextNumberConverter.DecodeStringToInt(encodedGameId);
            lifecycle.ObserveRoot(encodedGameId, nativeSeed);
            MelonLogger.Msg(
                $"[Toolkit RNG] Root observed; corrective RNG not attached. GAME ID={encodedGameId}, root=0x{NativeRunSeed.ToRootSeed(nativeSeed):X8}.");
        }
        catch (Exception exception)
        {
            lifecycle.Clear();
            MelonLogger.Warning($"[Toolkit RNG] Root observation failed; corrective RNG remains detached: {exception.Message}");
        }
    }
}

/// <summary>Immediately invalidates corrective state when native progress is cleaned.</summary>
[HarmonyPatch(typeof(ProgressDataService), nameof(ProgressDataService.Clean))]
internal static class CorrectiveRngProgressCleanPatch
{
    private static void Prefix()
    {
        CorrectiveRngLifecycle? lifecycle = ToolkitMod.CorrectiveRng;
        if (lifecycle is null || lifecycle.Phase == CorrectiveRngLifecyclePhase.Detached)
            return;

        lifecycle.Clear();
        MelonLogger.Msg("[Toolkit RNG] Native progress clean observed; corrective RNG session cleared.");
    }
}

[HarmonyPatch(typeof(RandomService), nameof(RandomService.RestoreSavedSeeds))]
internal static class CorrectiveRngLoadedRunAttachmentPatch
{
    private static void Postfix()
    {
        CorrectiveRngLifecycle? lifecycle = ToolkitMod.CorrectiveRng;
        if (lifecycle is null || lifecycle.Phase != CorrectiveRngLifecyclePhase.RootObserved)
            return;

        try
        {
            var coordinator = new CorrectiveRngAttachmentCoordinator(
                lifecycle,
                new FileCorrectiveRngPersistenceStore());
            CorrectiveRngAttachmentResult result =
                coordinator.Attach(CorrectiveRngGamePersistencePaths.Resolve());

            switch (result.Status)
            {
                case CorrectiveRngAttachmentStatus.LegacyAttached:
                    MelonLogger.Msg("[Toolkit RNG] Loaded legacy run attached with pristine corrective streams; no persistence written.");
                    break;
                case CorrectiveRngAttachmentStatus.CorrectiveRestored:
                    MelonLogger.Msg("[Toolkit RNG] Corrective RNG snapshot restored and attached.");
                    break;
                default:
                    MelonLogger.Warning(
                        $"[Toolkit RNG] Corrective RNG attachment blocked ({result.Status}): {result.Detail}");
                    break;
            }
        }
        catch (Exception exception)
        {
            MelonLogger.Warning(
                $"[Toolkit RNG] Loaded-run attachment failed; corrective RNG remains unavailable: {exception.Message}");
        }
    }
}


[HarmonyPatch(typeof(SaveSystem), "SaveToStorage", typeof(string), typeof(string))]
internal static class CorrectiveRngNativeSavePersistencePatch
{
    private static void Postfix(string __1)
    {
        if (!string.Equals(__1, "Progress.gd", StringComparison.OrdinalIgnoreCase))
            return;

        CorrectiveRngLifecycle? lifecycle = ToolkitMod.CorrectiveRng;
        if (lifecycle is null || lifecycle.Phase != CorrectiveRngLifecyclePhase.Attached)
            return;

        try
        {
            var coordinator = new CorrectiveRngPersistenceCoordinator(
                lifecycle,
                new FileCorrectiveRngPersistenceStore());
            CorrectiveRngPersistencePaths paths = CorrectiveRngGamePersistencePaths.Resolve();
            if (!coordinator.IsCorrectiveRequired(paths))
                return;

            coordinator.PersistAfterNativeSave(paths);
            MelonLogger.Msg("[Toolkit RNG] Corrective RNG sidecar persisted for native Progress.gd generation.");
        }
        catch (Exception exception)
        {
            MelonLogger.Error(
                $"[Toolkit RNG] Native Progress.gd saved but corrective RNG sidecar persistence failed: {exception}");
        }
    }
}

[HarmonyPatch(typeof(RandomService), nameof(RandomService.Range), typeof(RandomType), typeof(int), typeof(int))]
internal static class CorrectiveNeutralRangePatch
{
    private static bool Prefix(RandomType __0, int __1, int __2, ref int __result)
    {
        RandomType randomType = __0;
        int min = __1;
        int max = __2;
        if (randomType != RandomType.Zone || CorrectiveNeutralPlacement.NativeSpawnDepth <= 0 ||
            !CorrectiveNeutralPlacement.IsActive)
            return true;

        if (min == -2 && max == 2)
        {
            // Vanilla position selection is obsolete for corrective preview placement.
            // Keep the native coroutine moving without consuming RandomType.Zone.
            CorrectiveNeutralPlacement.PositionComponent++;
            __result = 0;
            return false;
        }

        if (min == 0 && !CorrectiveNeutralPlacement.IdentityReturnedToNative &&
            CorrectiveNeutralPlacement.IdentityIndex is int identity && max >= identity)
        {
            CorrectiveNeutralPlacement.IdentityReturnedToNative = true;
            __result = identity;
            return false;
        }

        if (min == 0 && max == 3 &&
            CorrectiveNeutralPlacement.IdentityReturnedToNative &&
            CorrectiveNeutralPlacement.OrientationIndex is int orientation)
        {
            __result = orientation;
            return false;
        }

        return true;
    }
}


[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.IsNeutralBuildingToPlace))]
internal static class CorrectiveNeutralExistenceScopePatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix()
    {
        if (CorrectiveNeutralPlacement.ExistenceRoll is not null)
            CorrectiveNeutralPlacement.ExistenceNativeDepth++;
    }

    private static void Postfix(bool __result)
    {
        if (!__result && CorrectiveNeutralPlacement.ExistenceRoll is not null)
            CorrectiveNeutralPlacement.Complete();
    }

    private static Exception? Finalizer(Exception? __exception)
    {
        if (CorrectiveNeutralPlacement.ExistenceNativeDepth > 0)
            CorrectiveNeutralPlacement.ExistenceNativeDepth--;
        return __exception;
    }
}

[HarmonyPatch(typeof(RandomService), nameof(RandomService.Range), typeof(RandomType), typeof(float), typeof(float))]
internal static class CorrectiveNeutralExistenceRangePatch
{
    private static bool Prefix(RandomType __0, float __1, float __2, ref float __result)
    {
        if (__0 != RandomType.Zone || CorrectiveNeutralPlacement.ExistenceNativeDepth <= 0 ||
            CorrectiveNeutralPlacement.ExistenceRoll is not float roll)
            return true;

        __result = Math.Clamp(roll, __1, __2);
        return false;
    }
}


[HarmonyPatch(typeof(PlaceTheNewZoneContext), nameof(PlaceTheNewZoneContext.CancelZonePlacement))]
internal static class CorrectiveNeutralCancelPatch
{
    private static void Postfix()
    {
        NeutralPreviewProbe.DestroyVisualPreview();
        CorrectiveNeutralPlacement.Complete();
        NeutralPreviewProbe.Pending = null;
    }
}

[HarmonyPatch(typeof(PlaceTheNewZoneContext), nameof(PlaceTheNewZoneContext.Stop))]
internal static class CorrectiveNeutralStopPatch
{
    private static void Postfix()
    {
        NeutralPreviewProbe.DestroyVisualPreview();
        CorrectiveNeutralPlacement.Complete();
        NeutralPreviewProbe.Pending = null;
    }
}

[HarmonyPatch(typeof(PlaceTheNewZoneContext), "IsPlaceValidForNeutralBuilding")]
internal static class CorrectiveNeutralObsoletePositionValidationPatch
{
    private static bool Prefix(ref bool __result)
    {
        if (CorrectiveNeutralPlacement.NativeSpawnDepth <= 0 || !CorrectiveNeutralPlacement.IsActive)
            return true;

        __result = true;
        return false;
    }
}
