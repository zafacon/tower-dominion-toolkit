using MelonLoader;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TowerDominion.Toolkit.Game;

/// <summary>Read-only setup indicators. Selection remains console-only.</summary>
internal sealed class ChallengePreRunOverlay
{
    private readonly ChallengeRegistry registry;
    private readonly ScreenIndicator commander;
    private readonly ScreenIndicator frontier;
    private float nextLookup;
    private float nextDiagnostic;

    internal ChallengePreRunOverlay(ChallengeRegistry registry)
    {
        this.registry = registry;
        commander = new ScreenIndicator("Commander", () => frontier?.Hide());
        frontier = new ScreenIndicator("Frontier", () => commander.Hide());
    }

    internal void Draw()
    {
        var selections = ToolkitMod.SavedChallenges;
        bool visible = selections != null && !selections.RunActive && selections.Selected.Count > 0;
        if (!visible) { commander.Hide(); frontier.Hide(); return; }
        if (Time.unscaledTime >= nextLookup)
        {
            nextLookup = Time.unscaledTime + 1.5f;
            try
            {
                var view = CommanderChallengeAnchorPatch.Current;
                if (view != null)
                {
                    var anchor = ChallengePreRunAnchors.CommanderNext(view);
                    if (anchor != null && view.nextButton.alpha > 0.5f)
                        commander.Attach(anchor);
                }
                if (Time.unscaledTime >= nextDiagnostic)
                {
                    nextDiagnostic = Time.unscaledTime + 15f;
                    MelonLogger.Msg($"[Toolkit Challenges] Commander lookup: selected={selections!.Selected.Count}, "
                        + $"view={(view == null ? "missing" : "captured")}, "
                        + $"anchor={(view == null ? "missing" : (ChallengePreRunAnchors.CommanderNext(view) == null ? "missing" : "present"))}.");
                }
                var menu = FrontierChallengeAnchorPatch.Current;
                if (menu != null && menu.root != null && menu.root.gameObject.activeInHierarchy)
                {
                    var anchor = ChallengePreRunAnchors.FrontierStart(menu);
                    if (anchor != null) frontier.Attach(anchor);
                }
            }
            catch (Exception ex) { MelonLogger.Warning($"[Toolkit Challenges] Pre-run indicator lookup: {ex.Message}"); }
        }
        string description = ChallengeDisplay.Describe(registry, selections!.Selected);
        var commanderView = CommanderChallengeAnchorPatch.Current;
        bool commanderActive = commanderView != null && commanderView.nextButton != null &&
            commanderView.nextButton.gameObject.activeInHierarchy && commanderView.nextButton.alpha > 0.5f;
        var frontierMenu = FrontierChallengeAnchorPatch.Current;
        bool frontierActive = frontierMenu != null && frontierMenu.root != null &&
            frontierMenu.root.gameObject.activeInHierarchy;
        if (frontierActive) commander.Hide();
        else if (commanderActive) frontier.Hide();
        if (commanderActive && !frontierActive) commander.Draw(selections.Selected.Count, description);
        if (frontierActive) frontier.Draw(selections.Selected.Count, description);
    }

    private sealed class ScreenIndicator
    {
        private readonly string name;
        private readonly Action hideOther;
        private RectTransform? anchor;
        private RectTransform? indicator;
        private ChallengePopup? popup;

        internal ScreenIndicator(string name, Action hideOther)
        {
            this.name = name;
            this.hideOther = hideOther;
        }

        internal void Attach(RectTransform target)
        {
            if (anchor == target && indicator != null && popup != null) return;
            Destroy();
            var parent = target.parent?.GetComponent<RectTransform>();
            if (parent == null)
            {
                MelonLogger.Warning($"[Toolkit Challenges] {name}: anchor {target.name} has no RectTransform parent.");
                return;
            }
            anchor = target;
            var go = new GameObject("Toolkit" + name + "ChallengesIndicator");
            go.transform.SetParent(parent, false);
            indicator = go.AddComponent<RectTransform>();
            var layout = go.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.type = Image.Type.Simple;
            var border = go.AddComponent<Outline>();
            border.effectColor = Color.black;
            border.effectDistance = new Vector2(5f, -5f);
            border.useGraphicAlpha = false;
            var button = go.AddComponent<Button>();
            if (!ChallengeIndicatorIcon.Apply(go.transform, image, 78f))
                MelonLogger.Warning($"[Toolkit Challenges] {name}: threat_icon unavailable; showing white fallback button.");
            MelonLogger.Msg($"[Toolkit Challenges] {name}: attached to {target.name} under {parent.name}.");
            // Keep the popup in the same active UI layer as the working button.
            // Convert screen center to the Footer's local coordinates rather than
            // anchoring it to the Footer's far-right edge or the root canvas.
            popup = new ChallengePopup(parent, "SELECTED CHALLENGES");
            popup.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            popup.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            popup.Rect.pivot = new Vector2(0.5f, 0.5f);
            popup.Rect.sizeDelta = new Vector2(1100f, 520f);
            popup.Rect.SetAsLastSibling();
            button.onClick.AddListener((UnityAction)(() =>
            {
                hideOther();
                popup?.Toggle();
            }));
        }

        internal void Draw(int count, string descriptions)
        {
            if (anchor == null || indicator == null || popup == null ||
                !anchor.gameObject.activeInHierarchy || !anchor.gameObject.scene.IsValid())
            { Hide(); return; }
            indicator.gameObject.SetActive(true);
            indicator.anchorMin = anchor.anchorMin;
            indicator.anchorMax = anchor.anchorMax;
            indicator.pivot = anchor.pivot;
            indicator.sizeDelta = new Vector2(128f, 128f);
            indicator.anchoredPosition = anchor.anchoredPosition + new Vector2(-90f, anchor.rect.height + 85f);
            var parentRect = popup.Rect.parent?.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                var canvas = parentRect.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect,
                    new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), camera, out var center))
                    popup.Rect.anchoredPosition = center - parentRect.rect.center;
            }
            popup.Refresh(true, descriptions);
        }

        internal void Hide()
        {
            if (indicator != null) indicator.gameObject.SetActive(false);
            popup?.Hide();
        }

        private void Destroy()
        {
            if (indicator != null) UnityEngine.Object.Destroy(indicator.gameObject);
            if (popup != null) UnityEngine.Object.Destroy(popup.Root);
            indicator = null;
            popup = null;
            anchor = null;
        }

    }
}
