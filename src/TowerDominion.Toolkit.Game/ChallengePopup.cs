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
    private readonly RectTransform scrollContent;
    private readonly Scrollbar scrollbar;
    private readonly ScrollRect scrollRect;
    private const float PanelWidth = 850f;
    private const float TextWidth = 680f;

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
        Rect.sizeDelta = new Vector2(PanelWidth, 520f);
        Rect.anchoredPosition = new Vector2(-119f, -255f);
        var heading = AddText(root.transform, "Title", title, 39f, new Vector2(0.055f, 1f), new Vector2(0.86f, 1f));
        heading.rectTransform.pivot = new Vector2(0.5f, 1f);
        heading.rectTransform.offsetMin = new Vector2(0f, -83f);
        heading.rectTransform.offsetMax = new Vector2(0f, -16f);
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        heading.fontStyle = FontStyles.Bold;
        heading.enableWordWrapping = false;
        heading.overflowMode = TextOverflowModes.Ellipsis;
        var viewportObject = new GameObject("DescriptionsViewport");
        viewportObject.transform.SetParent(root.transform, false);
        var viewport = viewportObject.AddComponent<RectTransform>();
        viewport.anchorMin = new Vector2(0.055f, 0f);
        viewport.anchorMax = new Vector2(0.89f, 1f);
        viewport.offsetMin = new Vector2(0f, 22f);
        viewport.offsetMax = new Vector2(0f, -100f);
        var viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
        viewportImage.raycastTarget = true;
        viewportObject.AddComponent<RectMask2D>();
        var contentObject = new GameObject("DescriptionsScrollContent");
        contentObject.transform.SetParent(viewportObject.transform, false);
        scrollContent = contentObject.AddComponent<RectTransform>();
        scrollContent.anchorMin = new Vector2(0f, 1f);
        scrollContent.anchorMax = new Vector2(1f, 1f);
        scrollContent.pivot = new Vector2(0.5f, 1f);
        scrollContent.anchoredPosition = Vector2.zero;
        Content = AddText(contentObject.transform, "ChallengeDescriptions", "", 30f, Vector2.zero, Vector2.one);
        Content.alignment = TextAlignmentOptions.TopLeft;
        scrollRect = viewportObject.AddComponent<ScrollRect>();
        scrollRect.content = scrollContent;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;
        var scrollbarObject = new GameObject("DescriptionsScrollbar");
        scrollbarObject.transform.SetParent(root.transform, false);
        var scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(0.91f, 0f);
        scrollbarRect.anchorMax = new Vector2(0.91f, 1f);
        scrollbarRect.pivot = new Vector2(0.5f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(14f, -122f);
        scrollbarRect.anchoredPosition = new Vector2(0f, -39f);
        var track = scrollbarObject.AddComponent<Image>();
        track.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        var handleObject = new GameObject("Handle");
        handleObject.transform.SetParent(scrollbarObject.transform, false);
        var handleRect = handleObject.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        var handleImage = handleObject.AddComponent<Image>();
        handleImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
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

    internal bool IsVisible => expanded && Root != null && Root.activeInHierarchy;
    internal void Toggle() { expanded = !expanded; Root.SetActive(expanded); }
    internal void Hide() { expanded = false; if (Root != null) Root.SetActive(false); }
    internal void Refresh(bool visible, string descriptions)
    {
        if (!visible) Hide();
        else if (expanded)
        {
            Root.SetActive(true);
            if (Content.text != descriptions)
            {
                Content.text = descriptions;
                float contentHeight = Content.GetPreferredValues(descriptions, TextWidth, 0f).y + 18f;
                Rect.sizeDelta = new Vector2(PanelWidth, Mathf.Clamp(contentHeight + 122f, 420f, 760f));
                scrollContent.sizeDelta = new Vector2(0f, Mathf.Max(1f, contentHeight));
                scrollContent.anchoredPosition = Vector2.zero;
                scrollRect.verticalNormalizedPosition = 1f;
                scrollbar.gameObject.SetActive(contentHeight > Rect.sizeDelta.y - 122f);
            }
        }
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
