using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using Il2CppNvizzio.Game.GamePlay.Stats;

namespace TowerDominion.Toolkit.Game;

/// <summary>Include the staging ground's already-recorded tank damage in its native wave total.</summary>
[HarmonyPatch(typeof(BuildingEntity), nameof(BuildingEntity.GetDamageDoneThisWave))]
internal static class ArmoredStagingGroundDamageDisplayPatch
{
    private const string BuildingDataName = "F201";
    private const string TankEntityType = "F2V_Tank";

    private static void Postfix(BuildingEntity __instance, ref float __result)
    {
        // Native aggregation includes HQ and F301 only. Do not change either,
        // and do not create a child statistics entry just to display a zero.
        if (__instance?.Data?.name != BuildingDataName ||
            !__instance.IsChildLiveStatPresent(TankEntityType)) return;

        var tankDamage = __instance.GetChildLiveStat(TankEntityType, LiveStatType.DamageDone);
        if (tankDamage != null) __result += tankDamage.ValueThisWave;
    }
}
