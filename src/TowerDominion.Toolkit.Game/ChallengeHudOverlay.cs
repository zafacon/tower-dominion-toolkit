using Il2CppTMPro;
using MelonLoader;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TowerDominion.Toolkit.Game;

/// <summary>Native Unity HUD button and active-run challenge panel.</summary>
internal sealed class ChallengeHudOverlay
{
    private readonly ChallengeRegistry registry;
    private RectTransform? nativeButton;
    private ChallengePopup? popup;
    private float nextLookup;
    private float nextPositionDiagnostic;
    private RectTransform? cachedTech;
    private RectTransform? cachedLifebar;
    private float nextPositionLookup;
    private float nextHudRetry;

    internal ChallengeHudOverlay(ChallengeRegistry registry) => this.registry = registry;
    internal bool IsPopupVisible => popup?.IsVisible == true;

    internal void Draw()
    {
        var selection = ToolkitMod.SavedChallenges;
        bool visible = selection?.RunActive == true && selection.Active.Count > 0;
        if (!visible)
        {
            popup?.Hide();
            HideNativeButton();
            InvalidatePositionReferences();
            return;
        }
        if (Time.unscaledTime < nextHudRetry) return;
        try
        {
            EnsureNativeUi();
            UpdatePosition();
            if (nativeButton != null) nativeButton.gameObject.SetActive(true);
            popup?.Refresh(true, ChallengeDisplay.Describe(registry,
                ChallengeDisplay.VisibleIds(selection!, inRun: true)));
        }
        catch (Il2CppInterop.Runtime.Il2CppException exception)
        {
            // The post-game inspection scene may destroy the gameplay header
            // before MainLoopContext.Stop releases the run snapshot.
            MelonLogger.Warning($"[Toolkit Challenges] Gameplay HUD unavailable: {exception.Message}");
            InvalidatePositionReferences();
            nextHudRetry = Time.unscaledTime + 2f;
            HideNativeButton();
            popup?.Hide();
        }
    }

    private void HideNativeButton()
    {
        try { if (nativeButton != null) nativeButton.gameObject.SetActive(false); }
        catch (Il2CppInterop.Runtime.Il2CppException) { nativeButton = null; popup = null; }
    }

    private void InvalidatePositionReferences()
    {
        cachedTech = null;
        cachedLifebar = null;
        nextPositionLookup = 0f;
    }

    private static void AddPanelLayer(Transform parent, string name, Color color,
        Vector2 offsetMin, Vector2 offsetMax, bool topAnchored = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = topAnchored ? new Vector2(0f, 1f) : Vector2.zero;
        rect.anchorMax = topAnchored ? new Vector2(1f, 1f) : Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
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

    // Center the button in the actual gap between the Tech bar and HQ lifebar.
    // FactionUI is NOT the HQ emblem: its bounds lie elsewhere in the header.
    private void UpdatePosition()
    {
        if (nativeButton == null) return;
        var right = nativeButton.parent as RectTransform;
        // Resolve the live Header by name: IL2CPP parent casts may return a
        // non-RectTransform wrapper even when the hierarchy is valid.
        if (Time.unscaledTime >= nextPositionLookup)
        {
            nextPositionLookup = Time.unscaledTime + 2f;
            cachedTech = null;
            cachedLifebar = null;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (candidate == null || !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.activeInHierarchy) continue;
                if (candidate.name == "TechBar" && candidate.parent?.name == "Resources")
                    cachedTech = candidate;
                if (candidate.name == "Lifebar" && candidate.parent?.name == "Header")
                    cachedLifebar = candidate;
            }
        }
        var tech = cachedTech;
        var lifebar = cachedLifebar;
        if (right == null || tech == null || lifebar == null ||
            !tech.gameObject.activeInHierarchy || !lifebar.gameObject.activeInHierarchy)
        {
            PositionDiagnostic($"Missing/inactive references: right={right != null} tech={tech != null} " +
                $"lifebar={lifebar != null} techActive={tech?.gameObject.activeInHierarchy} " +
                $"lifebarActive={lifebar?.gameObject.activeInHierarchy}");
            return;
        }

        // All measurements below are in Header-local UI units, not world space.
        // Header spans 3840 design units; RightPanel starts at 60% of its width.
        // World corners are zero in this IL2CPP HUD, so never use them here.
        float headerWidth = 3840f;
        float resourcesX = 230f;
        float techRight = resourcesX + tech.anchoredPosition.x + tech.rect.width;
        float lifebarLeft = headerWidth * 0.5f + lifebar.anchoredPosition.x -
            lifebar.rect.width * lifebar.pivot.x;
        float gap = lifebarLeft - techRight;
        float required = nativeButton.rect.width + 12f;
        if (gap < required)
        {
            PositionDiagnostic($"REJECTED local gap={gap:F1} required={required:F1} " +
                $"techRight={techRight:F1} lifebarLeft={lifebarLeft:F1}");
            return;
        }
        // RightPanel is anchored at x=0.6 of Header and y=top.
        float rightOriginX = headerWidth * right.anchorMin.x;
        float centerX = (techRight + lifebarLeft) * 0.5f;
        float centerY = -72f + tech.anchoredPosition.y - tech.rect.height * 0.5f;
        nativeButton.anchoredPosition = new Vector2(centerX - rightOriginX, centerY);
        PositionDiagnostic($"ACCEPTED local techRight={techRight:F1} lifebarLeft={lifebarLeft:F1} " +
            $"gap={gap:F1} button={nativeButton.anchoredPosition}");
        if (popup != null)
            popup.Rect.anchoredPosition = new Vector2(nativeButton.anchoredPosition.x - 64f,
                nativeButton.anchoredPosition.y - 76f);
    }

    private void PositionDiagnostic(string message)
    {
        if (Time.unscaledTime < nextPositionDiagnostic) return;
        nextPositionDiagnostic = Time.unscaledTime + 10f;
        MelonLogger.Msg($"[Toolkit Challenge Position] {message}");
    }

    private void EnsureNativeUi()
    {
        if (nativeButton != null && popup != null) return;
        if (Time.unscaledTime < nextLookup) return;
        nextLookup = Time.unscaledTime + 2f;
        try
        {
            foreach (var right in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (right == null || !right.gameObject.scene.IsValid() || right.name != "RightPanel" || !right.gameObject.activeInHierarchy) continue;
                Transform? expert = right.Find("ExpertModeUI/Button");
                if (expert == null) continue;
                Image? source = expert.GetComponent<Image>();
                if (source == null) continue;
                if (nativeButton == null)
                {
                    Image clone = UnityEngine.Object.Instantiate(source, right);
                    clone.gameObject.name = "ToolkitChallengeIndicator";
                    // Do not let RightPanel's layout include the injected control.
                    var indicatorLayout = clone.GetComponent<LayoutElement>();
                    if (indicatorLayout == null) indicatorLayout = clone.gameObject.AddComponent<LayoutElement>();
                    indicatorLayout.ignoreLayout = true;
                    var button = clone.GetComponent<Button>();
                    if (button == null) { UnityEngine.Object.Destroy(clone.gameObject); continue; }
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener((UnityAction)(() => popup?.Toggle()));
                    clone.raycastTarget = true;
                    clone.color = source.color;
                    for (int i = 0; i < clone.transform.childCount; i++) clone.transform.GetChild(i).gameObject.SetActive(false);
                    nativeButton = clone.rectTransform;
                    nativeButton.anchorMin = new Vector2(0f, 1f);
                    nativeButton.anchorMax = new Vector2(0f, 1f);
                    nativeButton.pivot = new Vector2(0.5f, 0.5f);
                    nativeButton.sizeDelta = new Vector2(128f, 128f);
                    nativeButton.anchoredPosition = new Vector2(-55f, -120f);
                    // Reuse the wave counter's native threat icon, inverted to a
                    // black silhouette on a white button background.
                    Image? waveIcon = null;
                    foreach (var candidate in Resources.FindObjectsOfTypeAll<Image>())
                    {
                        if (candidate == null || candidate.sprite == null ||
                            candidate.sprite.name != "threat_icon" ||
                            candidate.transform.parent?.name != "Button" ||
                            candidate.transform.parent?.parent?.name != "ThreatReportUI") continue;
                        waveIcon = candidate;
                        break;
                    }
                    if (waveIcon != null)
                    {
                        clone.color = Color.white;
                        var iconObject = new GameObject("ChallengeWaveIcon");
                        iconObject.transform.SetParent(clone.transform, false);
                        var iconRect = iconObject.AddComponent<RectTransform>();
                        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                        iconRect.pivot = new Vector2(0.5f, 0.5f);
                        iconRect.anchoredPosition = Vector2.zero;
                        iconRect.sizeDelta = new Vector2(78f, 78f);
                        var icon = iconObject.AddComponent<Image>();
                        icon.sprite = waveIcon.sprite;
                        icon.type = Image.Type.Simple;
                        icon.preserveAspect = true;
                        icon.color = Color.black;
                        icon.raycastTarget = false;
                    }
                    else
                        MelonLogger.Warning("[Toolkit Challenges] Native threat_icon sprite not found; challenge button remains blank.");
                }
                if (popup == null) popup = new ChallengePopup(right, "ACTIVE CHALLENGES");
                MelonLoader.MelonLogger.Msg("[Toolkit Challenges] Native HUD button and panel attached.");
                return;
            }
        }
        catch (Exception ex) { MelonLoader.MelonLogger.Warning($"[Toolkit Challenges] Native UI unavailable: {ex.Message}"); }
    }
}
