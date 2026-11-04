using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TowerDominion.Toolkit.Game;

/// <summary>Shared read-only native-styled popup, instantiated separately on each screen.</summary>
internal sealed class ChallengePopup
{
    internal GameObject Root { get; }
    internal RectTransform Rect { get; }
    internal TextMeshProUGUI Content { get; }
    private bool expanded;

    internal ChallengePopup(RectTransform parent, string title)
    {
        var root = new GameObject("ToolkitChallengesPopup");
        root.transform.SetParent(parent, false);
        Root = root;
        Rect = root.AddComponent<RectTransform>();
        var layout = root.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        var image = root.AddComponent<Image>();
        foreach (var candidate in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (candidate == null || candidate.sprite == null || candidate.sprite.name != "border_32" ||
                candidate.name != "Background" || candidate.transform.parent?.name != "Panel") continue;
            image.sprite = candidate.sprite;
            image.type = candidate.type;
            image.pixelsPerUnitMultiplier = candidate.pixelsPerUnitMultiplier;
            break;
        }
        image.color = new Color(0.078f, 0.078f, 0.078f, 1f);
        image.raycastTarget = true;
        Rect.anchorMin = new Vector2(0f, 1f);
        Rect.anchorMax = new Vector2(0f, 1f);
        Rect.pivot = new Vector2(0f, 1f);
        Rect.sizeDelta = new Vector2(650f, 400f);
        Rect.anchoredPosition = new Vector2(-119f, -255f);
        var heading = AddText(root.transform, "Title", title, 39f, new Vector2(0.055f, 0.78f), new Vector2(0.83f, 0.97f));
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        heading.fontStyle = FontStyles.Bold;
        Content = AddText(root.transform, "ChallengeDescriptions", "", 30f, new Vector2(0.055f, 0.07f), new Vector2(0.94f, 0.76f));
        Content.alignment = TextAlignmentOptions.TopLeft;
        var closeObject = new GameObject("CloseButton");
        closeObject.transform.SetParent(root.transform, false);
        var closeRect = closeObject.AddComponent<RectTransform>();
        var closeImage = closeObject.AddComponent<Image>();
        closeImage.color = Color.white;
        Image? nativeClose = null;
        foreach (var candidate in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (candidate == null || candidate.sprite?.name != "button_8" || candidate.name != "Close" ||
                candidate.transform.parent?.name != "Header" || candidate.transform.parent?.parent?.name != "Panel") continue;
            nativeClose = candidate;
            break;
        }
        if (nativeClose != null)
        {
            closeImage.sprite = nativeClose.sprite;
            closeImage.type = nativeClose.type;
            closeImage.pixelsPerUnitMultiplier = nativeClose.pixelsPerUnitMultiplier;
            var nativeIcon = nativeClose.transform.Find("Icon")?.GetComponent<Image>();
            if (nativeIcon != null && nativeIcon.sprite != null)
            {
                var iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(closeObject.transform, false);
                var iconRect = iconObject.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(48f, 48f);
                var iconImage = iconObject.AddComponent<Image>();
                iconImage.sprite = nativeIcon.sprite;
                iconImage.color = Color.black;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
        }
        var close = closeObject.AddComponent<Button>();
        close.onClick.AddListener((UnityAction)Hide);
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-18f, -18f);
        closeRect.sizeDelta = new Vector2(90f, 64f);
        if (nativeClose == null)
        {
            var label = AddText(closeObject.transform, "CloseLabel", "×", 40f, Vector2.zero, Vector2.one);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.black;
        }
        Root.SetActive(false);
    }

    internal void Toggle() { expanded = !expanded; Root.SetActive(expanded); }
    internal void Hide() { expanded = false; if (Root != null) Root.SetActive(false); }
    internal void Refresh(bool visible, string descriptions)
    {
        if (!visible) Hide();
        else if (expanded) { Root.SetActive(true); Content.text = descriptions; }
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, string value, float size, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        var rect = text.rectTransform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }
}
