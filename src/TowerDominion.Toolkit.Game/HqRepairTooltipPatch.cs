using System.Globalization;
using System.Text.RegularExpressions;
using HarmonyLib;
using Il2CppNvizzio.Core.Singleton;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using Il2CppNvizzio.Game.GamePlay.Paths;
using Il2CppNvizzio.Game.Services;
using Il2CppNvizzio.Game.UI.Tooltip;
using MelonLoader;
using Il2CppStringList = Il2CppSystem.Collections.Generic.List<string>;

namespace TowerDominion.Toolkit.Game;

[HarmonyPatch(typeof(UniversalTooltipTrigger), "GetHQRepair")]
internal static class HqRepairTooltipPatch
{
    private const int HpRepairType = 8;
    private const int ExternalGameEffectType = 39;

    private static readonly Regex PercentRegex =
        new(@"\(\+(?<value>-?\d+(?:[\.,]\d+)?)%\)", RegexOptions.Compiled);

    private static readonly Regex HpRegex =
        new(@"<b>(?<value>-?\d+(?:[\.,]\d+)?)\s*<style=PropertyDescription>",
            RegexOptions.Compiled);

    private static void Postfix(UniversalTooltipTrigger __instance, ref Il2CppStringList __result)
    {
        try
        {
            if (__result == null || __result.Count == 0) return;

            float externalPercent = GetExternalRepairPercent();
            if (externalPercent <= 0f) return;

            var hq = MonoSingleton<EntityManager>.Instance?.GetHQEntity();
            if (hq == null) return;

            for (int i = 0; i < __result.Count; i++)
            {
                string line = __result[i];
                var percentMatch = PercentRegex.Match(line);
                var hpMatch = HpRegex.Match(line);
                if (!percentMatch.Success || !hpMatch.Success) continue;
                if (!TryParse(percentMatch.Groups["value"].Value, out float nativePercent)) continue;

                float correctedPercent = nativePercent + externalPercent;
                float correctedHp = __instance.Round(hq.MaxHp * correctedPercent / 100f, 0, 0.01f);

                line = ReplaceGroup(line, percentMatch.Groups["value"],
                    correctedPercent.ToString("0.##", CultureInfo.InvariantCulture));

                hpMatch = HpRegex.Match(line);
                if (!hpMatch.Success) return;
                line = ReplaceGroup(line, hpMatch.Groups["value"],
                    correctedHp.ToString("0.##", CultureInfo.InvariantCulture));

                __result[i] = line;
                return;
            }

            MelonLogger.Warning("HQ repair tooltip: external repair found, but native repair line format was not recognized.");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"HQ repair tooltip correction skipped: {ex.Message}");
        }
    }

    private static float GetExternalRepairPercent()
    {
        var doctrines = Services.SkillService?.OwnedDoctrines;
        var paths = MonoSingleton<PathsManager>.Instance;
        if (doctrines == null || paths == null) return 0f;

        float total = 0f;
        for (int i = 0; i < doctrines.Count; i++)
        {
            var doctrine = doctrines[i];
            var created = doctrine?.effectsCreated;
            if (created == null) continue;

            for (int j = 0; j < created.Count; j++)
            {
                var parent = created[j];
                if (parent?.Data == null ||
                    (int)parent.Data.GameEffectType != ExternalGameEffectType ||
                    parent.ExternalGameEffects == null)
                    continue;

                int scale = paths.DistinctPathCountFromHQ;
                if (scale <= 0) continue;

                for (int k = 0; k < parent.ExternalGameEffects.Count; k++)
                {
                    var child = parent.ExternalGameEffects[k];
                    if (child?.Data == null || (int)child.Data.GameEffectType != HpRepairType) continue;
                    var math = child.Data.MathEffect;
                    if (math == null) continue;

                    float contribution = math.value * scale;
                    total += contribution;
                }
            }
        }

        return total;
    }

    private static bool TryParse(string value, out float result) =>
        float.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static string ReplaceGroup(string source, Group group, string replacement) =>
        source[..group.Index] + replacement + source[(group.Index + group.Length)..];
}
