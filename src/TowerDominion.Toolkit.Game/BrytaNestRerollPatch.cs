using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.Contexts;
using Il2CppNvizzio.Game.GamePlay.Zones;
using Il2CppNvizzio.Game.Services.Currencies;
using Il2CppNvizzio.Game.Services;
using Il2CppNvizzio.Game.UI.Buttons;
using Il2CppNvizzio.Game.UI.CurrencyBars;
using Il2CppNvizzio.Game.UI.Views;
using MelonLoader;
using Il2CppNvizzio.Game.Input;

namespace TowerDominion.Toolkit.Game;

internal static class BrytaNestRerollScope
{
    internal static bool NestViewActive;
    internal static bool Busy;
    internal static ZoneShape? CurrentShape;
    internal static ZoneExpansionView? View;
    [ThreadStatic] internal static bool Reroll;
    [ThreadStatic] internal static bool NestGenerator;

    internal static bool Enabled => BrytaRerollDiagnostics.Enabled;
}

[HarmonyPatch(typeof(ZoneExpansionView), nameof(ZoneExpansionView.Initialize))]
internal static class BrytaNestRerollViewPatch
{
    private static void Postfix(ZoneExpansionView __instance)
    {
        BrytaNestRerollScope.NestViewActive = __instance.data?.IsNestZone == true;
        if (!BrytaNestRerollScope.Enabled || !BrytaNestRerollScope.NestViewActive) return;
        BrytaNestRerollScope.View = __instance;
        var reroll = __instance.rerollButton;
        if (reroll == null) return;
        var component = reroll.GetComponent<ZoneRerollButton>();
        if (component?.currencyReference == null || component.cost == null) return;
        bool affordable = component.currencyReference.Get(CurrencySpace.Live) >= component.cost.Quantity;
        reroll.interactable = affordable && !BrytaNestRerollScope.Busy;
        __instance.requestCursorChange?.SetCursor(affordable ? CursorType.Arrow : CursorType.Block);
        if (__instance.tooltipDataTunnel != null) __instance.tooltipDataTunnel.enabled = false;
        if (__instance.tooltipDataRegular != null) __instance.tooltipDataRegular.enabled = true;
        BrytaRerollDiagnostics.Report("nest-ui", $"affordable={affordable}");
    }
}

[HarmonyPatch(typeof(PlaceTheNewZoneContext), nameof(PlaceTheNewZoneContext.RerollZone))]
internal static class BrytaNestRerollScopePatch
{
    private static void Prefix() => BrytaNestRerollScope.Reroll =
        BrytaNestRerollScope.Enabled && BrytaNestRerollScope.NestViewActive;
    private static void Postfix() => BrytaNestRerollScope.Reroll = false;
    private static Exception? Finalizer(Exception? __exception)
    {
        BrytaNestRerollScope.Reroll = false;
        BrytaNestRerollScope.NestGenerator = false;
        return __exception;
    }
}

[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.InstantiateNewPlaceZoneContextData))]
internal static class BrytaNestRerollRedirectPatch
{
    private static bool Prefix(ZoneManager __instance, bool excludePreviousZone, ref PlaceZoneContextData __result)
    {
        if (!BrytaNestRerollScope.Reroll || !excludePreviousZone) return true;
        BrytaNestRerollScope.NestGenerator = true;
        try
        {
            __result = __instance.InstantiateNewPlaceNestZoneContextData(true);
            BrytaRerollDiagnostics.Report("nest-redirect", BrytaRerollDiagnostics.ZoneInfo(__result));
            return false;
        }
        finally
        {
            BrytaNestRerollScope.NestGenerator = false;
        }
    }
}

[HarmonyPatch(typeof(ZoneFactory), nameof(ZoneFactory.InstantiateRandomNestZone),
    new[] { typeof(UnityEngine.Transform), typeof(bool), typeof(Il2CppSystem.Collections.Generic.HashSet<ZoneShape>), typeof(Zone) })]
internal static class BrytaNestRerollFactoryPatch
{
    private static void Prefix(ref bool isExcludeShape, ref Il2CppSystem.Collections.Generic.HashSet<ZoneShape> excludeShapes)
    {
        if (!BrytaNestRerollScope.NestGenerator || BrytaNestRerollScope.CurrentShape is not { } shape) return;
        var isolated = new Il2CppSystem.Collections.Generic.HashSet<ZoneShape>();
        isolated.Add(shape);
        isolated.Add(ZoneShape.NoPath);
        excludeShapes = isolated;
        isExcludeShape = true;
        BrytaRerollDiagnostics.Report("nest-exclusion", $"excluding {shape} and NoPath");
    }

    private static void Postfix(Zone __result)
    {
        if (!BrytaNestRerollScope.NestGenerator || __result == null) return;
        var shape = __result.ZoneShape;
        BrytaRerollDiagnostics.Report("nest-candidate", $"shape={shape}");
    }
}

[HarmonyPatch(typeof(ZoneExpansionView), nameof(ZoneExpansionView.DestroyView))]
internal static class BrytaNestRerollViewCleanupPatch
{
    private static void Prefix() { BrytaNestRerollScope.NestViewActive = false; BrytaNestRerollScope.View = null; }
}


/// <summary>Intercept only nest clicks. Never execute native button pre-reroll side effects.</summary>
[HarmonyPatch(typeof(ZoneRerollButton), nameof(ZoneRerollButton.OnButtonClick))]
internal static class BrytaNestSafeClickPatch
{
    private static bool Prefix(ZoneRerollButton __instance)
    {
        if (!BrytaNestRerollScope.Enabled || !BrytaNestRerollScope.NestViewActive)
            return true;

        if (BrytaNestRerollScope.Busy) return false;
        var currency = __instance.currencyReference;
        var cost = __instance.cost;
        if (currency == null || cost == null || __instance.button == null ||
            !__instance.button.interactable || cost.Quantity <= 0 ||
            currency.Get(CurrencySpace.Live) < cost.Quantity)
            return false;

        if (BrytaNestRerollScope.CurrentShape == null) return false;
        var pending = Services.ProgressDataService?.ProgressSaveData?.ZoneToPlace;
        if (pending == null) return false;
        using var branch = BrytaNativeAcceptance.TryEnter();
        if (branch == null) return false;
        BrytaNestRerollScope.Busy = true;
        __instance.button.interactable = false;
        try
        {
            // Generate before charging: a failed native generation must not consume currency.
            // The scoped redirect preserves native preview replacement and SetData lifecycle.
            BrytaRerollDiagnostics.Report("safe-click", "begin native nest reroll without original button handler");
            BrytaRerollDiagnostics.Report("pending-nest", $"clearing saved id={pending.ZoneId}");
            pending.ZoneId = string.Empty;
            PlaceTheNewZoneContext.RerollZone();
            currency.Add(CurrencySpace.Live, -cost.Quantity);
            var live = currency.Get(CurrencySpace.Live);
            var bar = CurrencyBar.GetByType(cost.Type);
            if (bar != null && bar.currentValueText != null)
            {
                bar.currentValueText.text = live.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                BrytaRerollDiagnostics.Report("nest-display", $"updated label={live}, linkedCurrencyPresent={bar.linkedCurrency != null}");
            }
            BrytaRerollDiagnostics.Report("safe-click", $"completed, charged={cost.Quantity}");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Toolkit Bryta] Nest reroll failed; original button handler suppressed: {ex}");
        }
        finally
        {
            BrytaNestRerollScope.Busy = false;
            __instance.button.interactable = currency.Get(CurrencySpace.Live) >= cost.Quantity;
            BrytaNestRerollScope.View?.requestCursorChange?.SetCursor(__instance.button.interactable ? CursorType.Arrow : CursorType.Block);
        }
        return false;
    }
}

[HarmonyPatch(typeof(PlaceTheNewZoneContext), nameof(PlaceTheNewZoneContext.SetData))]
internal static class BrytaCurrentShapePatch
{
    private static void Prefix(PlaceZoneContextData data) => BrytaNestRerollScope.CurrentShape = data?.ZoneToPlace?.ZoneShape;
}

/// <summary>Read the game's mapped action after native input state is updated.</summary>
[HarmonyPatch(typeof(InputManager), nameof(InputManager.Update))]
internal static class BrytaRerollMappedInputPatch
{
    private static void Postfix(InputManager __instance)
    {
        if (!BrytaNestRerollScope.Enabled || !BrytaNestRerollScope.NestViewActive || BrytaNestRerollScope.Busy) return;
        var button = BrytaNestRerollScope.View?.rerollButton;
        if (button == null || !button.isActiveAndEnabled || !button.IsInteractable()) return;
        if (!__instance.IsClicked("reroll")) return;
        var component = button.GetComponent<ZoneRerollButton>();
        if (component == null) return;
        BrytaRerollDiagnostics.Report("nest-keyboard", "native mapped reroll action");
        component.OnButtonClick();
    }
}
