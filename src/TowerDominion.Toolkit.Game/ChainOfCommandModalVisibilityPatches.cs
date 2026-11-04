using HarmonyLib;
using Il2CppNvizzio.Game.UI.Views;

namespace TowerDominion.Toolkit.Game;

// Track native view transitions rather than FindObjectOfType, which throws
// Method unstripping failed for these IL2CPP view types in this game build.
internal static class ChainOfCommandModalVisibility
{
    internal static bool WaveEnd;
    internal static bool SpecialReward;
    internal static bool Pause;
    internal static bool IsOpen => WaveEnd || SpecialReward || Pause;
    internal static void Reset() { WaveEnd = false; SpecialReward = false; Pause = false; }
}

[HarmonyPatch(typeof(WaveEndView), "TransitionIn")]
internal static class ChainWaveEndEnterPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.WaveEnd = true;
}

[HarmonyPatch(typeof(WaveEndView), "TransitionOut")]
internal static class ChainWaveEndExitPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.WaveEnd = false;
}

// [HarmonyPatch(typeof(SpecialRewardView), "SetViewData")]
internal static class ChainSpecialRewardEnterPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.SpecialReward = true;
}

[HarmonyPatch(typeof(SpecialRewardView), "Complete")]
internal static class ChainSpecialRewardExitPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.SpecialReward = false;
}

[HarmonyPatch(typeof(PauseView), "TransitionIn")]
internal static class ChainPauseEnterPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.Pause = true;
}

[HarmonyPatch(typeof(PauseView), "ContinueGame")]
internal static class ChainPauseExitPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.Pause = false;
}
