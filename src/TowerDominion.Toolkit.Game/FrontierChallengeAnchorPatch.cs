using HarmonyLib;
using Il2CppNvizzio.Game.UI.Views;

namespace TowerDominion.Toolkit.Game;

/// <summary>Capture the actual Frontier menu instance on initialization.</summary>
[HarmonyPatch(typeof(ExpertModeMenu), nameof(ExpertModeMenu.Initialize))]
internal static class FrontierChallengeAnchorPatch
{
    internal static ExpertModeMenu? Current { get; private set; }
    private static void Postfix(ExpertModeMenu __instance) => Current = __instance;
}
