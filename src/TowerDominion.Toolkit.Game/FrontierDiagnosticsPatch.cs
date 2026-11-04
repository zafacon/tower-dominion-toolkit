using System.Collections;
using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Doctrines;
using Il2CppNvizzio.Game.UI;
using Il2CppNvizzio.Game.UI.Views;
using MelonLoader;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Observational probes only. Never mutate toggles, scores, or owned skills.</summary>
internal static class FrontierDiagnostics
{
    internal static bool Enabled { get; set; }

    internal static void Report(string phase, ExpertModeMenu menu)
    {
        if (!Enabled) return;
        try
        {
            MelonLogger.Msg($"[Toolkit Frontier] {phase}: score={menu.currentScore}, level={menu.currentLevelFloat}, "
                + $"startInteractable={menu.startButton?.interactable}, startActive={menu.startButton?.gameObject?.activeInHierarchy}");
        }
        catch (Exception exception) { MelonLogger.Warning($"[Toolkit Frontier] {phase} diagnostic failed: {exception.Message}"); }
    }
}

[HarmonyPatch(typeof(ExpertModeMenu), nameof(ExpertModeMenu.Initialize))]
internal static class FrontierMenuInitializeProbe
{
    private static void Postfix(ExpertModeMenu __instance)
    {
        FrontierStartGuard.BeginRestore(__instance);
        FrontierDiagnostics.Report("initialized", __instance);
        if (ToolkitMod.SavedFrontiers is { SelectedIds.Count: > 0 })
            MelonCoroutines.Start(RestoreAfterInitialize(__instance));
        else FrontierStartGuard.EndRestore(__instance);
    }

    private static IEnumerator RestoreAfterInitialize(ExpertModeMenu menu)
    {
        // Allow native setup to complete before raising any toggle event.
        yield return null;
        FrontierSelections? saved = ToolkitMod.SavedFrontiers;
        if (saved is null || saved.SelectedIds.Count == 0) { FrontierStartGuard.EndRestore(menu); yield break; }
        int applied = 0;
        try
        {
            if (menu == null || menu.root == null || !menu.root.gameObject.activeInHierarchy) yield break;
            ChallengeUI[] toggles = menu.root!.GetComponentsInChildren<ChallengeUI>(true);
            foreach (ChallengeUI toggle in toggles)
            {
                if (toggle == null || toggle.Data == null || toggle.styledToggle == null) continue;
                if (!saved.IsSelected(toggle.Data.UniqueID) || toggle.styledToggle.isOn) continue;
                // Unity's isOn setter invokes the native callback; never invoke it manually.
                toggle.styledToggle.isOn = true;
                applied++;
            }
            MelonLogger.Msg($"[Toolkit Frontier] Restored {applied} of {saved.SelectedIds.Count} saved modifiers; game score={menu.currentScore}, startInteractable={menu.startButton?.interactable}.");
        }
        catch (Exception exception)
        {
            MelonLogger.Error($"[Toolkit Frontier] Restore failed after {applied} modifiers: {exception}");
        }
        finally { FrontierStartGuard.EndRestore(menu); }
    }
}

[HarmonyPatch(typeof(ExpertModeMenu), nameof(ExpertModeMenu.Clear))]
internal static class FrontierMenuClearProbe
{
    private static void Postfix(ExpertModeMenu __instance) { FrontierStartGuard.Reset(__instance); FrontierDiagnostics.Report("cleared", __instance); }
}

[HarmonyPatch(typeof(ExpertModeMenu), "OnToggleChallengeUI")]
internal static class FrontierMenuToggleProbe
{
    private static void Prefix(ExpertModeMenu __instance, bool isOn, SkillData skillData)
    {
        if (!FrontierDiagnostics.Enabled) return;
        MelonLogger.Msg($"[Toolkit Frontier] toggle request: id={skillData?.UniqueID} on={isOn}");
        FrontierDiagnostics.Report("before-toggle", __instance);
    }

    private static void Postfix(ExpertModeMenu __instance) { FrontierStartGuard.MarkScoreChanged(__instance); FrontierDiagnostics.Report("after-toggle", __instance); }
}

[HarmonyPatch(typeof(ChallengeUI), nameof(ChallengeUI.SetData))]
internal static class FrontierToggleSetupProbe
{
    private static void Postfix(ChallengeUI __instance, SkillData skillData)
    {
        if (!FrontierDiagnostics.Enabled) return;
        MelonLogger.Msg($"[Toolkit Frontier] toggle setup: id={skillData?.UniqueID} isOn={__instance.styledToggle?.isOn}");
    }
}
