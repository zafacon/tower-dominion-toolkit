using HarmonyLib;
using Il2CppNvizzio.Game.UI.Views;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

// Keep validated transitions; inspect reward/defeat UI without patching native initialization.
internal static class ChainOfCommandModalVisibility
{
    internal static bool WaveEnd;
    internal static bool SpecialReward;
    internal static bool Pause;
    internal static bool Settings;
    internal static bool GameOver;
    private static float scanAt;
    internal static bool IsOpen => WaveEnd || SpecialReward || Pause || Settings || GameOver;
    internal static void Reset() { WaveEnd = false; SpecialReward = false; Pause = false; Settings = false; GameOver = false; scanAt = 0f; }

    internal static void SampleViews()
    {
        if (Time.unscaledTime < scanAt) return;
        scanAt = Time.unscaledTime + 0.25f;
        bool reward = false, gameOver = false;
        foreach (var rect in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rect == null) continue;
            string name = rect.name;
            bool isReward = name == "SpecialRewardView" || name == "SpecialRewardView(Clone)";
            bool isGameOver = name == "GameOverView" || name == "GameOverView(Clone)";
            if ((!isReward && !isGameOver) || !rect.gameObject.scene.IsValid() || !rect.gameObject.activeInHierarchy) continue;
            reward |= isReward;
            gameOver |= isGameOver;
        }
        if (reward != SpecialReward || gameOver != GameOver)
            MelonLoader.MelonLogger.Msg($"[ChainOfCommand visibility] hierarchy reward={reward} gameOver={gameOver}");
        SpecialReward = reward;
        GameOver = gameOver;
    }
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


[HarmonyPatch(typeof(SettingsWindow), "Initialize")]
internal static class ChainSettingsEnterPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.Settings = true;
}

[HarmonyPatch(typeof(SettingsWindow), "CloseWindow")]
internal static class ChainSettingsExitPatch
{
    private static void Prefix() => ChainOfCommandModalVisibility.Settings = false;
}


// GameOverView hooks intentionally omitted: forfeit initialization regressed
// during testing. Revalidate native defeat flow before adding visibility hooks.
