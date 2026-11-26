using HarmonyLib;
using UnityEngine.EventSystems;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

[HarmonyPatch(typeof(Il2Cpp.StyledToggle), nameof(Il2Cpp.StyledToggle.OnPointerClick))]
internal static class BlueprintBagInputPatch
{
    private static bool Prefix(Il2Cpp.StyledToggle __instance, PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Right) return true;
        try { return !BlueprintBagPrototype.TryStoreToggle(__instance); }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[Blueprint Bag] Pointer handler unavailable: {ex.Message}");
            return true;
        }
    }
}
