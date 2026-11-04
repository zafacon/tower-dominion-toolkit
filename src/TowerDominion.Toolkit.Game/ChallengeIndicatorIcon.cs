using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDominion.Toolkit.Game;

/// <summary>Use the same black threat silhouette on white for every challenge indicator.</summary>
internal static class ChallengeIndicatorIcon
{
    internal static bool Apply(Transform button, Image background, float iconSize)
    {
        Sprite? source = null;
        // Sprite assets can be loaded without an instantiated Image; this is
        // important on pre-run screens where the in-run threat widget is absent.
        foreach (var candidate in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (candidate != null && candidate.name == "threat_icon")
            {
                source = candidate;
                break;
            }
        }
        if (source == null)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (candidate != null && candidate.sprite?.name == "threat_icon")
                {
                    source = candidate.sprite;
                    break;
                }
            }
        }
        if (source == null)
        {
            var labelObject = new GameObject("ChallengeFallbackIcon");
            labelObject.transform.SetParent(button, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "!";
            label.fontSize = iconSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.black;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return true;
        }
        background.color = Color.white;
        var go = new GameObject("ChallengeWaveIcon");
        go.transform.SetParent(button, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(iconSize, iconSize);
        var icon = go.AddComponent<Image>();
        icon.sprite = source;
        icon.preserveAspect = true;
        icon.color = Color.black;
        icon.raycastTarget = false;
        return true;
    }
}
