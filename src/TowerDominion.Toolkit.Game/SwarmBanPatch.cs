using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Characters;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Threats;
using Il2CppNvizzio.Game.GamePlay.Threats;
using Il2CppNvizzio.Game.Services.Threats;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>Temporarily filter the native candidate pool before seeded swarm selection.</summary>
[HarmonyPatch(typeof(EnemySwarmService), nameof(EnemySwarmService.SelectPredeterminedSwarm))]
internal static class SwarmBanPatch
{
    private sealed record Removed(int Index, EnemySwarmData Swarm);

    private static bool IsBanned(string characterId, bool finalWave)
    {
        var bans = ToolkitMod.SavedBans;
        if (bans == null) return false;
        string category = finalWave ? "waves" : "foes";
        if (bans.IsBanned(category, characterId)) return true;
        try
        {
            CharacterData? character = CharacterData.ResourceLoadEnemy(characterId);
            return character != null && bans.IsBanned(category, character.UniqueID);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[Toolkit Bans] Character lookup failed for {characterId}: {ex.Message}");
            return false;
        }
    }

    private static void Prefix(ThreatLevel threatLevel, out List<Removed>? __state)
    {
        __state = null;
        if (ToolkitMod.SavedBans == null || threatLevel?.Data?.enemySwarms == null) return;
        var source = threatLevel.Data.enemySwarms;
        var removed = new List<Removed>();
        for (int i = 0; i < source.Count; i++)
        {
            var swarm = source[i];
            if (swarm != null && IsBanned(swarm.CharacterId, threatLevel.IsFinalWave)) removed.Add(new Removed(i, swarm));
        }
        if (removed.Count == 0) return;
        int available = source.Count - removed.Count;
        if (available < threatLevel.NbSwarmToSelect)
        {
            MelonLogger.Warning($"[Toolkit Bans] Cannot enforce {removed.Count} exclusions: only {available} candidates for {threatLevel.NbSwarmToSelect} required. Native pool retained.");
            return;
        }
        for (int i = removed.Count - 1; i >= 0; i--) source.RemoveAt(removed[i].Index);
        __state = removed;
        MelonLogger.Msg($"[Toolkit Bans] Excluded {removed.Count} {(threatLevel.IsFinalWave ? "wave" : "foe")} candidates from native selection.");
    }

    private static void Postfix(ThreatLevel threatLevel, List<Removed>? __state)
    {
        if (__state == null || threatLevel?.Data?.enemySwarms == null) return;
        var source = threatLevel.Data.enemySwarms;
        foreach (var removed in __state) source.Insert(removed.Index, removed.Swarm);
    }
}
