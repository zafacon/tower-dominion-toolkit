using HarmonyLib;
using Il2CppNvizzio.Game.UI;
using MelonLoader;
using TowerDominion.Toolkit.Model;
using Il2CppTMPro;
using UnityEngine;

namespace TowerDominion.Toolkit.Game;

/// <summary>Clones the native version label's appearance without changing the game's version text.</summary>
[HarmonyPatch(typeof(VersionNumberUI), "Start")]
internal static class ToolkitVersionLabelPatch
{
    private const string LabelName = "TowerDominionToolkitVersionLabel";

    private static void Postfix(VersionNumberUI __instance)
    {
        try
        {
            TextMeshProUGUI? original = __instance.label;
            if (original == null || original.transform.parent == null) return;
            Transform parent = original.transform.parent;
            if (parent.Find(LabelName) != null) return;

            TextMeshProUGUI clone = UnityEngine.Object.Instantiate(original, parent);
            clone.gameObject.name = LabelName;
            // If the native controller shares the label's GameObject, do not duplicate its lifecycle.
            VersionNumberUI? nativeController = clone.GetComponent<VersionNumberUI>();
            if (nativeController != null) UnityEngine.Object.Destroy(nativeController);

            clone.text = $"TK: {ToolkitBuildInfo.Version}";
            clone.raycastTarget = false;
            RectTransform nativeRect = original.rectTransform;
            RectTransform cloneRect = clone.rectTransform;
            cloneRect.anchorMin = nativeRect.anchorMin;
            cloneRect.anchorMax = nativeRect.anchorMax;
            cloneRect.pivot = nativeRect.pivot;
            cloneRect.sizeDelta = nativeRect.sizeDelta;
            // Match the native baseline and position our right-aligned text to its left.
            float nativeTextWidth = original.preferredWidth;
            cloneRect.anchoredPosition = nativeRect.anchoredPosition + new Vector2(-nativeTextWidth - 12f, 0f);
            cloneRect.localScale = nativeRect.localScale;
            cloneRect.localRotation = nativeRect.localRotation;
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[Toolkit] Could not add main-menu version label: {exception.Message}");
        }
    }
}
