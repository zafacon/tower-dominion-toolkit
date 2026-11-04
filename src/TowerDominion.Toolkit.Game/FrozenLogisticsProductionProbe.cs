using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.Currencies;
using Il2CppNvizzio.Game.GamePlay.Events;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using Il2CppNvizzio.Core.Singleton;
using Il2CppNvizzio.Game.GamePlay.GameEffects;
using MelonLoader;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Cap recurring round income without changing the underlying production upgrades.</summary>
[HarmonyPatch(typeof(RecurringTransaction), nameof(RecurringTransaction.ApplyTransactionData))]
internal static class FrozenLogisticsProductionPatch
{
    private static readonly Dictionary<string, long> StartingProduction = new(StringComparer.Ordinal);
    private static bool wasActive;
    private static bool baselineCaptured;

    internal static bool TryCapDisplay(string currency, long value, out long capped)
    {
        capped = value;
        var selections = ToolkitMod.SavedChallenges;
        if (selections?.RunActive != true ||
            !selections.Active.Contains(FrozenLogisticsRule.Id, StringComparer.OrdinalIgnoreCase) ||
            !baselineCaptured || !StartingProduction.TryGetValue(currency, out long baseline)) return false;
        capped = ProductionCap.Apply(value, baseline);
        return true;
    }

    private static void Prefix(RecurringTransaction __instance, out GameEffectValue? __state)
    {
        __state = null;
        var selections = ToolkitMod.SavedChallenges;
        bool active = selections?.RunActive == true &&
            selections.Active.Contains(FrozenLogisticsRule.Id, StringComparer.OrdinalIgnoreCase);
        if (!active)
        {
            if (wasActive)
            {
                StartingProduction.Clear();
                baselineCaptured = false;
            }
            wasActive = false;
            return;
        }

        wasActive = true;
        try
        {
            var data = __instance.TransactionData;
            if (data?.Data == null) return;
            if (data.GameplayEvent == GameplayEvent.GameBegin)
            {
                // GameBegin follows the initial map setup, before the first round award.
                // Snapshot both currencies from actual production, not starting grants.
                if (data.Data.Type.ToString() == "Supply") baselineCaptured = false;
                if (!baselineCaptured)
                {
                    var manager = MonoSingleton<EntityManager>.Instance;
                    if (manager == null) return;
                    long supply = manager.GetBuildingProduction(GameplayEvent.RoundBegin,
                        Il2CppNvizzio.Game.Services.Currencies.CurrencyType.Supply);
                    long recon = manager.GetBuildingProduction(GameplayEvent.RoundBegin,
                        Il2CppNvizzio.Game.Services.Currencies.CurrencyType.Reconnaissance);
                    StartingProduction.Clear();
                    StartingProduction["Supply"] = supply;
                    StartingProduction["Reconnaissance"] = recon;
                    baselineCaptured = true;
                    MelonLogger.Msg($"[Toolkit Frozen Logistics] GameBegin baseline: Supply={supply}, Recon={recon}");
                }
                return;
            }
            if (data.GameplayEvent != GameplayEvent.RoundBegin || !baselineCaptured) return;
            var currency = data.Data.Type.ToString();
            if (currency != "Supply" && currency != "Reconnaissance") return;
            var quantity = __instance.Quantity;
            if (quantity == null) return;
            long current = quantity.LongValue;
            long baseline = StartingProduction[currency];
            long capped = ProductionCap.Apply(current, baseline);
            if (capped == current) return;
            // Restore the transaction's original quantity after the award, preserving upgrades.
            __state = new GameEffectValue(quantity);
            __instance.SetQuantity(new GameEffectValue((float)capped));
            MelonLogger.Msg($"[Toolkit Frozen Logistics] {currency} recurring award capped: {current} -> {capped}");
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[Toolkit Frozen Logistics] Failed to cap award: {exception.Message}");
        }
    }

    private static void Postfix(RecurringTransaction __instance, GameEffectValue? __state)
    {
        if (__state != null) __instance.SetQuantity(__state);
    }

    private static Exception? Finalizer(RecurringTransaction __instance, GameEffectValue? __state, Exception? __exception)
    {
        if (__exception != null && __state != null) __instance.SetQuantity(__state);
        return __exception;
    }
}
