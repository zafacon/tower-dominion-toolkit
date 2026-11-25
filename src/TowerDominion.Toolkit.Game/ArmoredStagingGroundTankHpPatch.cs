using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Enums;
using Il2CppNvizzio.Game.GamePlay.GameEffects;
using Il2CppNvizzio.Game.GamePlay.GameEntities;

namespace TowerDominion.Toolkit.Game;

[HarmonyPatch(typeof(CharacterEntity), nameof(CharacterEntity.IsTargetByGameEffect))]
internal static class ArmoredStagingGroundTankHpPatch
{
    private const string TankDataName = "F2V_Tank";
    private const string ArmoredStagingGroundDataName = "F201";
    private const string Hp1250EffectId = "bea6bd65-be14-4d47-a95f-cae73eb753b5";
    private const string Hp2500EffectId = "9c1c91c5-6664-4921-be7d-318705813ab1";

    private static void Postfix(
        CharacterEntity __instance,
        GameEffectTarget target,
        GameEffect effect,
        ref bool __result)
    {
        if (__result ||
            target != GameEffectTarget.ChildOfAnotherBuilding ||
            effect?.Data?.GameEffectType != GameEffectType.HpBonus ||
            __instance?.CharacterData?.name != TankDataName ||
            effect.Owner?.TryCast<BuildingEntity>() is not { } owner ||
            owner.Data?.name != ArmoredStagingGroundDataName)
        {
            return;
        }

        var effectId = effect.Data.UniqueID;
        if (effectId == Hp1250EffectId || effectId == Hp2500EffectId)
        {
            __result = true;
        }
    }
}
