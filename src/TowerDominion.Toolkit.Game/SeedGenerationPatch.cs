using HarmonyLib;
using Il2CppNvizzio.Game.Services.Utils;
using MelonLoader;
using TowerDominion.Toolkit.Runtime;

namespace TowerDominion.Toolkit.Game;

[HarmonyPatch(typeof(RandomService), nameof(RandomService.GenerateSeeds))]
internal static class SeedGenerationPatch
{
    internal static RandomService? PendingVerificationService { get; private set; }

    internal static void ClearPendingVerification() => PendingVerificationService = null;

    private static void Postfix(RandomService __instance, ref string __result)
    {
        SeedRuntimeBridge? bridge = ToolkitMod.SeedBridge;
        if (bridge is null)
        {
            return;
        }

        string generated = __result;
        string effective = bridge.OnSeedGenerated(generated, requested =>
        {
            if (int.TryParse(requested, out int numeric))
            {
                __instance.SetSeed(numeric);
            }
            else
            {
                __instance.SetEncodedSeed(requested);
            }

            return __instance.GetEncodedSeed();
        });

        __result = effective;
        if (bridge.AwaitingVerification)
        {
            PendingVerificationService = __instance;
        }
        if (effective != generated)
        {
            MelonLogger.Msg($"[Toolkit] Seed override applied: {effective}");
        }
    }
}
