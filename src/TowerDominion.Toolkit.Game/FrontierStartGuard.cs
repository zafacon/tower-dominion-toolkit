using HarmonyLib;
using Il2CppNvizzio.Game.UI.Views;
using MelonLoader;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

/// <summary>Prevent launching while native Frontier score animation or saved-toggle restoration is incomplete.</summary>
internal static class FrontierStartGuard
{
    private static System.IntPtr current;
    private static bool restoring;
    private static float changedAt;
    private static bool warned;

    internal static void BeginRestore(ExpertModeMenu menu)
    {
        current = menu.Pointer;
        restoring = true;
        changedAt = Time.unscaledTime;
        warned = false;
    }

    internal static void EndRestore(ExpertModeMenu menu)
    {
        if (menu != null && menu.Pointer == current)
        {
            restoring = false;
            changedAt = Time.unscaledTime;
        }
    }

    internal static void Reset(ExpertModeMenu menu)
    {
        if (menu != null && menu.Pointer == current) { restoring = false; current = System.IntPtr.Zero; }
    }

    internal static void MarkScoreChanged(ExpertModeMenu menu)
    {
        if (menu != null && menu.Pointer == current) changedAt = Time.unscaledTime;
    }

    internal static bool Ready(ExpertModeMenu menu)
    {
        if (menu == null || menu.Pointer != current) return true;
        if (restoring || Time.unscaledTime - changedAt < 0.35f) return false;
        return System.Math.Abs(menu.currentLevelFloat - menu.targetLevelFloat) < 0.015f;
    }

    internal static void Update()
    {
        var menu = FrontierChallengeAnchorPatch.Current;
        if (menu == null || menu.Pointer != current || menu.root == null || !menu.root.gameObject.activeInHierarchy) return;
        var button = menu.startButton;
        if (button == null) return;
        if (!Ready(menu)) button.interactable = false;
        // Once ready, native UpdateStartButton remains the authority for whether starting is allowed.
        else if (menu.currentScore > 0 && !button.interactable) button.interactable = true;
    }

    internal static bool AllowStart(ExpertModeMenu menu)
    {
        if (Ready(menu)) return true;
        if (!warned) { warned = true; MelonLogger.Msg("[Toolkit Frontier] Start blocked while modifier score is updating."); }
        return false;
    }
}

[HarmonyPatch(typeof(CharacterSelectionView), "OnClickStartButton")]
internal static class FrontierStartGuardPatch
{
    private static bool Prefix()
    {
        var menu = FrontierChallengeAnchorPatch.Current;
        return menu == null || menu.root == null || !menu.root.gameObject.activeInHierarchy || FrontierStartGuard.AllowStart(menu);
    }
}
