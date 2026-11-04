using HarmonyLib;
using Il2CppNvizzio.Game.Services.Currencies;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Keep Tech at zero while the run's Low Tech snapshot is active.</summary>
internal static class LowTechCurrencyGuard
{
    internal static bool Applies(CurrencyType type) => type == CurrencyType.Tech &&
        ToolkitMod.SavedChallenges is { RunActive: true } selections &&
        selections.Active.Contains(LowTechRule.Id, StringComparer.OrdinalIgnoreCase);
}

[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.Get),
    new[] { typeof(CurrencySpace), typeof(CurrencyType) })]
internal static class LowTechGetPatch
{
    private static void Postfix(CurrencyType currencyType, ref float __result)
    {
        if (LowTechCurrencyGuard.Applies(currencyType)) __result = 0;
    }
}

[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.GetMax),
    new[] { typeof(CurrencySpace), typeof(CurrencyType) })]
internal static class LowTechGetMaxPatch
{
    private static void Postfix(CurrencyType currencyType, ref float __result)
    {
        if (LowTechCurrencyGuard.Applies(currencyType)) __result = 0;
    }
}

[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.Set),
    new[] { typeof(CurrencySpace), typeof(CurrencyType), typeof(float) })]
internal static class LowTechSetSpacePatch
{
    private static void Prefix(CurrencyType currencyType, ref float value)
    {
        if (LowTechCurrencyGuard.Applies(currencyType)) value = 0;
    }
}

[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.Set),
    new[] { typeof(CurrencyType), typeof(float) })]
internal static class LowTechSetPatch
{
    private static void Prefix(CurrencyType currencyType, ref float value)
    {
        if (LowTechCurrencyGuard.Applies(currencyType)) value = 0;
    }
}

[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.Add),
    new[] { typeof(CurrencySpace), typeof(CurrencyType), typeof(float) })]
internal static class LowTechAddPatch
{
    private static void Prefix(CurrencyType currencyType, ref float value)
    {
        if (LowTechCurrencyGuard.Applies(currencyType)) value = 0;
    }
}

[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.AddMax),
    new[] { typeof(CurrencySpace), typeof(CurrencyType), typeof(float) })]
internal static class LowTechAddMaxPatch
{
    private static void Prefix(CurrencyType currencyType, ref float value)
    {
        if (LowTechCurrencyGuard.Applies(currencyType)) value = 0;
    }
}

/// <summary>Normalize both native currency spaces so the HUD cannot retain pre-run Tech.</summary>
[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.GetCurrency))]
internal static class LowTechNativeCurrencyPatch
{
    [ThreadStatic] private static bool normalizing;

    private static void Postfix(CurrencyType currencyType, Currency __result)
    {
        if (normalizing || __result == null || !LowTechCurrencyGuard.Applies(currencyType)) return;
        normalizing = true;
        try
        {
            if (__result.Get(CurrencySpace.Live) != 0 || __result.Get(CurrencySpace.Display) != 0)
                __result.SetAll(0f);
            if (__result.GetMax(CurrencySpace.Live) != 0 || __result.GetMax(CurrencySpace.Display) != 0)
                __result.SetAllMax(0f);
        }
        finally { normalizing = false; }
    }
}

/// <summary>Starting Tech uses the game's visual grant animation, which bypasses normal Add.</summary>
[HarmonyPatch(typeof(CurrenciesService), nameof(CurrenciesService.VisuallyAdd))]
internal static class LowTechVisualGrantPatch
{
    private static bool Prefix(CurrencyType currencyType)
    {
        // Suppress the entire visual grant for Tech, including its delayed display animation.
        return !LowTechCurrencyGuard.Applies(currencyType);
    }
}
