using HarmonyLib;
using Il2CppNvizzio.Game.Services.Currencies;
using Il2CppNvizzio.Game.UI.CurrencyBars;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>Show effective Frozen Logistics production without altering building statistics.</summary>
[HarmonyPatch(typeof(CurrencyBar), nameof(CurrencyBar.GetTotalProduction))]
internal static class FrozenLogisticsDisplayPatch
{
    private static void Postfix(CurrencyBar __instance, ref long __result)
    {
        try
        {
            var supplyBar = CurrencyBar.GetByType(CurrencyType.Supply);
            if (supplyBar != null && supplyBar.Pointer == __instance.Pointer)
            {
                if (FrozenLogisticsProductionPatch.TryCapDisplay("Supply", __result, out long capped))
                    __result = capped;
                return;
            }
            var reconBar = CurrencyBar.GetByType(CurrencyType.Reconnaissance);
            if (reconBar != null && reconBar.Pointer == __instance.Pointer &&
                FrozenLogisticsProductionPatch.TryCapDisplay("Reconnaissance", __result, out long reconCapped))
                __result = reconCapped;
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[Toolkit Frozen Logistics] Production display: {exception.Message}");
        }
    }

    // The native display updater does not call GetTotalProduction: it calculates
    // production a second time. Route it through the patched getter instead.
    [HarmonyPatch(typeof(CurrencyBar), nameof(CurrencyBar.UpdateProductionDisplay))]
    private static class ProductionDisplayUpdatePatch
    {
        private static bool Prefix(CurrencyBar __instance)
        {
            try
            {
                var supply = CurrencyBar.GetByType(CurrencyType.Supply);
                var recon = CurrencyBar.GetByType(CurrencyType.Reconnaissance);
                bool isSupply = supply != null && supply.Pointer == __instance.Pointer;
                bool isRecon = recon != null && recon.Pointer == __instance.Pointer;
                if (!isSupply && !isRecon) return true;

                long total = __instance.GetTotalProduction();
                if (!FrozenLogisticsProductionPatch.TryCapDisplay(
                    isSupply ? "Supply" : "Reconnaissance", total, out long capped)) return true;

                __instance.SetProductionValue(capped);
                return false;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning($"[Toolkit Frozen Logistics] Display update: {exception.Message}");
                return true;
            }
        }
    }
}
