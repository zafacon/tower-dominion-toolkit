using HarmonyLib;
using Il2CppNvizzio.Game.UI.Views;

namespace TowerDominion.Toolkit.Game;

/// <summary>Capture the native selection view when initialized; it is not a Unity component.</summary>
[HarmonyPatch(typeof(CharacterSelectionView), nameof(CharacterSelectionView.Initialize))]
internal static class CommanderChallengeAnchorPatch
{
    internal static CharacterSelectionView? Current { get; private set; }

    private static void Postfix(CharacterSelectionView __instance) => Current = __instance;
}
