using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Characters;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Threats;
using Il2CppNvizzio.Game.GamePlay.Threats;
using Il2CppNvizzio.Game.Services.Threats;
using MelonLoader;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>
/// Replace only nonmandatory native choices after normal selection. Never increase
/// the choice count or insert a swarm absent from the current threat's native pool.
/// </summary>
[HarmonyPatch(typeof(EnemySwarmService), nameof(EnemySwarmService.SelectPredeterminedSwarm))]
internal static class SwarmPinPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(EnemySwarmService __instance, ThreatLevel threatLevel)
    {
        var pins = ToolkitMod.SavedPins;
        if (pins == null || threatLevel?.Data?.enemySwarms == null || threatLevel.PredeterminedSwarms == null)
            return;

        string domain = threatLevel.IsFinalWave ? "waves" : "foes";
        var wanted = pins.List(domain);
        if (wanted.Count == 0) return;

        try
        {
            var selected = threatLevel.PredeterminedSwarms;
            var pool = threatLevel.Data.enemySwarms;
            // A native underfilled selection is not a license to create additional choices.
            if (selected.Count == 0 || selected.Count > threatLevel.NbSwarmToSelect) return;

            foreach (string id in wanted)
            {
                EnemySwarmData? target = null;
                for (int i = 0; i < pool.Count; i++)
                {
                    var swarm = pool[i];
                    if (swarm != null && Matches(swarm.CharacterId, id))
                    {
                        target = swarm;
                        break;
                    }
                }
                if (target == null)
                {
                    MelonLogger.Msg($"[Toolkit Pins] {domain} {id} is not in this threat level's native pool; skipped.");
                    continue;
                }

                bool present = false;
                for (int i = 0; i < selected.Count; i++)
                    if (Matches(selected[i].CharacterId, id)) { present = true; break; }
                if (present) continue;

                // The native Add method enforces this minimum, but checking
                // before removing a slot avoids a needless transient mutation.
                var runData = Il2CppNvizzio.Game.GamePlay.DataStructure.Settings.GlobalSettingsData.Instance?.RunData;
                if (runData == null || target.MinimumRegularWaveCount > runData.RegularWaveCount)
                {
                    MelonLogger.Msg($"[Toolkit Pins] {domain} {id} is not yet wave-eligible; skipped.");
                    continue;
                }

                // Never replace a mandatory encounter. The game's own Add method
                // checks the swarm's minimum regular wave count and duplicates.
                // Match IDs against the same stable IDs used by pin selections.
                var protectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < selected.Count; i++)
                    if (wanted.Any(pin => Matches(selected[i].CharacterId, pin)))
                        protectedIds.Add(selected[i].CharacterId);
                int replacementIndex = PinChoicePlanner.FindReplacement(
                    Enumerable.Range(0, selected.Count).Select(i => selected[i]).ToArray(),
                    protectedIds, swarm => swarm.CharacterId, swarm => swarm.IsMandatory);
                if (replacementIndex < 0)
                {
                    MelonLogger.Warning($"[Toolkit Pins] No replaceable encounter slot for {domain} {id}; mandatory and pinned choices preserved.");
                    continue;
                }
                // Do not displace anything if the native rule already excludes
                // this swarm from predetermined choices.
                if (__instance.IsSwarmPredetermined(target))
                {
                    MelonLogger.Msg($"[Toolkit Pins] {domain} {id} rejected by native duplicate-selection check.");
                    continue;
                }
                var displaced = selected[replacementIndex];
                int originalCount = selected.Count;
                selected.RemoveAt(replacementIndex);
                bool added = false;
                try
                {
                    added = threatLevel.AddPredeterminedSwarm(target);
                }
                catch (Exception exception)
                {
                    MelonLogger.Warning($"[Toolkit Pins] Rejected {domain} {id}: {exception.Message}");
                }
                finally
                {
                    if (!added)
                    {
                        // Restore even if the native add partially modified the list.
                        for (int i = selected.Count - 1; i >= 0; i--)
                            if (selected[i] == target) selected.RemoveAt(i);
                        selected.Insert(Math.Min(replacementIndex, selected.Count), displaced);
                    }
                }
                if (added)
                {
                    if (selected.Count != originalCount)
                    {
                        // A native add must not alter the number of choices.
                        selected.Remove(target);
                        selected.Insert(replacementIndex, displaced);
                        MelonLogger.Warning($"[Toolkit Pins] Rolled back unexpected choice count for {id}.");
                    }
                    else MelonLogger.Msg($"[Toolkit Pins] Included {domain} encounter {id} in native choice slots.");
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[Toolkit Pins] Native selection adjustment failed: {ex.Message}");
        }
    }

    private static bool Matches(string characterId, string pinnedId)
    {
        if (characterId.Equals(pinnedId, StringComparison.OrdinalIgnoreCase)) return true;
        var character = CharacterData.ResourceLoadEnemy(characterId);
        return character != null && character.UniqueID.Equals(pinnedId, StringComparison.OrdinalIgnoreCase);
    }
}
