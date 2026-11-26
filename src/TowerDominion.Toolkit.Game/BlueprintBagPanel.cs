using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

// Independent display: no cloned BlueprintUI, toggles or gameplay components.
internal sealed class BlueprintBagPanel
{
    private RectTransform? button;
    private RectTransform? panel;
    private RectTransform? content;
    private TextMeshProUGUI? counter;
    private TextMeshProUGUI? empty;
    private ScrollRect? scroll;
    private Scrollbar? scrollbar;
    private static RectTransform? wheelPanel;

    // A native hand may survive a run transition. Remove only our own controls
    // at the next initialized hand, when scene-owned transforms are safe to access.
    internal static void RemovePreviousControls(Transform? anchor)
    {
        wheelPanel = null;
        if (anchor == null) return;
        for (int i = anchor.childCount - 1; i >= 0; i--)
        {
            var child = anchor.GetChild(i);
            if (child == null || (child.name != "ToolkitBlueprintBagButton" && child.name != "ToolkitBlueprintBagPanel")) continue;
            child.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    internal static bool CapturesWheel()
    {
        if (ChallengeHudOverlay.TeardownStarted || ToolkitMod.SavedChallenges?.RunActive != true || wheelPanel == null || !wheelPanel.gameObject.activeInHierarchy) return false;
        var canvas = wheelPanel.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(wheelPanel, Input.mousePosition, camera);
    }
    private readonly List<GameObject> cells = new();
    private string signature = "";
    private bool open;
    private float nextRefresh;

    internal void Update(bool consoleOpen)
    {
        if (ChallengeHudOverlay.TeardownStarted || ToolkitMod.SavedChallenges?.RunActive != true)
        {
            button = null; panel = null; content = null; counter = null; empty = null; scroll = null; scrollbar = null; wheelPanel = null;
            cells.Clear(); signature = ""; open = false; nextRefresh = 0;
            return;
        }
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.2f;
        try
        {
            var hand = BlueprintBagPrototype.VisibleHand;
            bool visible = hand != null && !hand.InputFrozen && hand.selectedBlueprint == null && !consoleOpen;
            if (!visible)
            {
                if (button != null) button.gameObject.SetActive(false);
                if (panel != null) panel.gameObject.SetActive(false);
                open = false;
                return;
            }
            var anchor = hand!.layout?.GetComponent<RectTransform>();
            if (anchor == null || !anchor.gameObject.activeInHierarchy) return;
            if (button == null || panel == null) Create(anchor);
            if (button == null || panel == null || counter == null) return;
            button.gameObject.SetActive(true);
            var cards = BlueprintBagPrototype.DisplayCards();
            counter.text = $"BAG  {cards.Count}";
            panel.gameObject.SetActive(open);
            if (open) Refresh(cards);
        }
        catch (Exception ex)
        {
            nextRefresh = Time.unscaledTime + 3f;
            MelonLogger.Warning($"[Blueprint Bag UI] {ex.Message}");
        }
    }

    private void Create(RectTransform anchor)
    {
        Image? nativeClose = null;
        foreach (var candidate in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (candidate == null || candidate.sprite?.name != "button_8" || candidate.name != "Close" ||
                candidate.transform.parent?.name != "Header" || candidate.transform.parent?.parent?.name != "Panel") continue;
            nativeClose = candidate;
            break;
        }
        TMP_FontAsset? headingFont = null;
        foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (font == null || font.name != "BebasNeue-Regular SDF") continue;
            headingFont = font;
            break;
        }
        button = Rect(anchor, "ToolkitBlueprintBagButton", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(1, 0), new Vector2(-64, 12), new Vector2(180, 64));
        button.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var face = button.gameObject.AddComponent<Image>();
        StyleButton(face, nativeClose);
        var control = button.gameObject.AddComponent<Button>();
        control.targetGraphic = face;
        control.onClick.AddListener((UnityAction)(() => { open = !open; }));
        counter = Label(button, "Count", "BAG  0", 38, Vector2.zero, Vector2.one, Color.black);
        Heading(counter, headingFont);

        panel = Rect(anchor, "ToolkitBlueprintBagPanel", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(1, 0), new Vector2(-64, 88), new Vector2(1070, 740));
        panel.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(.078f, .078f, .078f, 1);
        foreach (var candidate in Resources.FindObjectsOfTypeAll<Image>())
        {
            if (candidate == null || candidate.sprite?.name != "border_32" || candidate.name != "Background") continue;
            background.sprite = candidate.sprite;
            background.type = candidate.type;
            background.pixelsPerUnitMultiplier = candidate.pixelsPerUnitMultiplier;
            break;
        }
        var title = Label(panel, "Title", "BLUEPRINT BAG", 48, new Vector2(.04f, .88f), new Vector2(.84f, .99f), Color.white);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        Heading(title, headingFont);
        var close = Rect(panel, "Close", Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-20, -16), new Vector2(90, 64));
        var closeImage = close.gameObject.AddComponent<Image>();
        StyleButton(closeImage, nativeClose);
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener((UnityAction)(() => { open = false; }));
        var nativeIcon = nativeClose?.transform.Find("Icon")?.GetComponent<Image>();
        if (nativeIcon != null && nativeIcon.sprite != null)
        {
            var center = new Vector2(.5f, .5f);
            var icon = Rect(close, "Icon", center, center, center, Vector2.zero, new Vector2(48, 48)).gameObject.AddComponent<Image>();
            icon.sprite = nativeIcon.sprite;
            icon.color = Color.black;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
        else Label(close, "X", "×", 38, Vector2.zero, Vector2.one, Color.black);

        var viewport = Rect(panel, "Viewport", new Vector2(.035f, .035f), new Vector2(.965f, .87f),
            new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
        viewport.gameObject.AddComponent<RectMask2D>();
        content = Rect(viewport, "Content", new Vector2(0, 1), Vector2.one,
            new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
        content.anchorMax = new Vector2(1, 1);
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 70;
        viewport.offsetMax = new Vector2(-30, 0);
        var trackRect = Rect(panel, "Scrollbar", new Vector2(.965f, .035f), new Vector2(.965f, .87f),
            new Vector2(1, .5f), Vector2.zero, new Vector2(18, 0));
        var track = trackRect.gameObject.AddComponent<Image>();
        track.color = new Color(.25f, .25f, .25f, 1);
        var area = Rect(trackRect, "SlidingArea", Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        var handle = Rect(area, "Handle", Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(.85f, .85f, .85f, 1);
        scrollbar = trackRect.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollbar.interactable = false;
        wheelPanel = panel;
        empty = Label(viewport, "Empty", "No stored blueprints", 30, Vector2.zero, Vector2.one, Color.white);
        Heading(empty, headingFont);
        panel.gameObject.SetActive(false);
        signature = "";
        MelonLogger.Msg($"[Blueprint Bag UI] attached to {anchor.name}, size={anchor.rect.size}");
    }

    private void Refresh(IReadOnlyList<BlueprintBagPrototype.DisplayCard> cards)
    {
        if (content == null || empty == null || scroll == null) return;
        string next = string.Join("|", cards.Select(c => $"{c.Id}:{c.Count}:{c.Cost}"));
        if (signature == next && cells.Count == cards.Count)
        {
            empty.gameObject.SetActive(cards.Count == 0);
            return;
        }
        signature = next;
        float position = scroll.verticalNormalizedPosition;
        foreach (var cell in cells) if (cell != null) { cell.SetActive(false); UnityEngine.Object.Destroy(cell); }
        cells.Clear();
        empty.gameObject.SetActive(cards.Count == 0);
        const int columns = 5;
        const float width = 186, height = 220, gap = 6;
        content.sizeDelta = new Vector2(0, Mathf.Max(1, (cards.Count + columns - 1) / columns * (height + gap)));
        if (scrollbar != null) scrollbar.interactable = content.rect.height > scroll.viewport.rect.height;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            var cell = Rect(content, "Stack", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(i % columns * (width + gap), -(i / columns) * (height + gap)), new Vector2(width, height));
            cells.Add(cell.gameObject);
            const float scale = 0.48f;
            Layer(cell, "Shadow", card.Shadow, 371, 413, 0, 0, scale, Color.black);
            var mask = Layer(cell, "BackgroundMask", card.Mask, 327, 369, 0, 0, scale, Color.white);
            if (card.Mask != null) mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Layer(mask.rectTransform, "Background", card.Background, 536, 374.35f, 7, -11, scale, Color.white);
            Layer(mask.rectTransform, "Illustration", card.Art, 540, 346.5f, 0, 10, scale, Color.white);
            Layer(cell, "Frame", card.Outline, 327, 369, 0, 0, scale, Color.white);
            var badgeImage = Layer(cell, "QuantityBadge", card.Badge, 110, 110, 85, 140, scale, Color.white);
            var count = Label(badgeImage.rectTransform, "Quantity", card.Count.ToString(), 75 * scale, Vector2.zero, Vector2.one, Color.black);
            if (card.Font != null) count.font = card.Font;
            count.fontStyle = FontStyles.Bold | FontStyles.Italic;
            // Independent hit target; no native blueprint selection callbacks.
            var hit = cell.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, .001f);
            var retrieve = cell.gameObject.AddComponent<Button>();
            retrieve.targetGraphic = hit;
            retrieve.transition = Selectable.Transition.None;
            retrieve.onClick.AddListener((UnityAction)(() =>
            {
                if (BlueprintBagPrototype.TryMouseTransfer(card.Id, false))
                {
                    signature = "";
                    nextRefresh = 0;
                }
            }));
        }
        scroll.verticalNormalizedPosition = cards.Count == 0 || float.IsNaN(position) ? 1f : Mathf.Clamp01(position);
    }

    private static void StyleButton(Image image, Image? native)
    {
        image.color = Color.white;
        if (native == null) return;
        image.sprite = native.sprite;
        image.type = native.type;
        image.pixelsPerUnitMultiplier = native.pixelsPerUnitMultiplier;
    }

    private static void Heading(TextMeshProUGUI text, TMP_FontAsset? font)
    {
        if (font != null) text.font = font;
        text.fontStyle = FontStyles.Bold | FontStyles.Italic;
    }

    private static Image Layer(Transform parent, string name, Image? source, float width, float height,
        float x, float y, float scale, Color color)
    {
        var center = new Vector2(.5f, .5f);
        var native = source?.rectTransform;
        var size = new Vector2(width, height);
        var position = new Vector2(x, y);
        if (native != null && name != "QuantityBadge")
        {
            size = native.rect.size;
            size = new Vector2(size.x * native.localScale.x, size.y * native.localScale.y);
            position = native.anchoredPosition;
        }
        var rect = Rect(parent, name, center, center, center, position * scale, size * scale);
        var image = rect.gameObject.AddComponent<Image>();
        if (source != null)
        {
            image.sprite = source.sprite;
            image.overrideSprite = source.overrideSprite;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier / scale;
            image.useSpriteMesh = source.useSpriteMesh;
            image.fillCenter = source.fillCenter;
            image.maskable = source.maskable;
            // Use the original base material, never the stencil-modified materialForRendering.
            image.material = source.material;
            // Do not inherit the unaffordable/frozen tint of the native hand card.
            image.color = new Color(color.r, color.g, color.b, source.color.a);
            if (native != null && name != "QuantityBadge") rect.localRotation = native.localRotation;
        }
        else image.color = Color.clear;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string value, float size,
        Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = value; text.fontSize = size; text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = min;
        text.rectTransform.anchorMax = max;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return text;
    }
}
