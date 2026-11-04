#if DEBUG
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDominion.Toolkit.Game;

/// <summary>Opt-in, read-only inspection of the native HUD hierarchy (F8).</summary>
internal static class ChallengeHudProbe
{
    internal static void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F8)) return;
        try
        {
            var all = Resources.FindObjectsOfTypeAll<RectTransform>();
            int matches = 0;
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid()) continue;
                string name = rect.name;
                if (name.IndexOf("frontier", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("expert", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("skull", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("challenge", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (++matches > 100) break;
                Describe(rect);
                for (int i = 0; i < rect.childCount && i < 12; i++)
                    if (rect.GetChild(i) is RectTransform child) Describe(child);
            }
            // Capture the native in-run HUD even when its names do not contain Frontier/Expert.
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid() || !rect.gameObject.activeInHierarchy) continue;
                if (!rect.name.Equals("RightPanel", StringComparison.OrdinalIgnoreCase)) continue;
                // Also inspect adjacent HUD branches: commander-specific controls may be
                // siblings of RightPanel rather than children of it.
                if (rect.parent is RectTransform header)
                {
                    MelonLogger.Msg("[Toolkit HUD Probe] === Header siblings ===");
                    Describe(header);
                    for (int h = 0; h < header.childCount && h < 40; h++)
                    {
                        if (header.GetChild(h) is not RectTransform branch) continue;
                        Describe(branch);
                        if (branch == rect) continue;
                        for (int j = 0; j < branch.childCount && j < 35; j++)
                            if (branch.GetChild(j) is RectTransform item) Describe(item);
                    }
                }
                MelonLogger.Msg("[Toolkit HUD Probe] === RightPanel hierarchy ===");
                Describe(rect);
                for (int i = 0; i < rect.childCount && i < 45; i++)
                {
                    if (rect.GetChild(i) is not RectTransform child) continue;
                    Describe(child);
                    for (int j = 0; j < child.childCount && j < 15; j++)
                        if (child.GetChild(j) is RectTransform grandchild) Describe(grandchild);
                }
            }
            // Inspect the game's own contextual HQ/Frontier panels for reusable
            // backgrounds, borders, typography and close-button sprites.
            int panels = 0;
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid() || !rect.gameObject.activeInHierarchy) continue;
                string name = rect.name;
                bool relevant = name.IndexOf("headquarter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("frontier", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("buildinginfo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("buildingdetail", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!relevant || ++panels > 15) continue;
                MelonLogger.Msg("[Toolkit Native Panel Probe] === " + name + " ===");
                Describe(rect);
                for (int i = 0; i < rect.childCount && i < 30; i++)
                {
                    if (rect.GetChild(i) is not RectTransform child) continue;
                    Describe(child);
                    for (int j = 0; j < child.childCount && j < 12; j++)
                        if (child.GetChild(j) is RectTransform grandchild) Describe(grandchild);
                }
            }
            // Inspect all images under the native HUD Header, including nested
            // wave-counter icons whose names are not necessarily wave-related.
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid() ||
                    !rect.gameObject.activeInHierarchy || rect.name != "Header" ||
                    rect.parent?.name != "PersistentGameHudView(Clone)") continue;
                MelonLogger.Msg("[Toolkit Wave Icon Probe] === Header image sprites ===");
                foreach (var image in rect.GetComponentsInChildren<Image>(true))
                {
                    if (image == null || image.sprite == null) continue;
                    MelonLogger.Msg($"[Toolkit Wave Icon Probe] path={GetPath(image.transform)} sprite={image.sprite.name} color={image.color}");
                }
            }
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid() ||
                    !rect.gameObject.activeInHierarchy || rect.name != "Header" ||
                    rect.parent?.name != "GameHUD(Clone)") continue;
                MelonLogger.Msg("[Toolkit Wave Counter Probe] === GameHUD Header sprites ===");
                foreach (var image in rect.GetComponentsInChildren<Image>(true))
                {
                    if (image == null || image.sprite == null) continue;
                    MelonLogger.Msg($"[Toolkit Wave Counter Probe] path={GetPath(image.transform)} sprite={image.sprite.name} color={image.color}");
                }
            }
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid() ||
                    !rect.gameObject.activeInHierarchy || rect.name != "SendWaveView(Clone)") continue;
                MelonLogger.Msg("[Toolkit Send Wave Probe] === SendWaveView descendants ===");
                foreach (var image in rect.GetComponentsInChildren<Image>(true))
                    if (image != null)
                        MelonLogger.Msg($"[Toolkit Send Wave Probe] path={GetPath(image.transform)} sprite={image.sprite?.name ?? "(none)"} color={image.color}");
                foreach (var text in rect.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (text != null)
                        MelonLogger.Msg($"[Toolkit Send Wave Probe] textpath={GetPath(text.transform)} text={text.text}");
            }
            // One-pass global inventory: all active scene UI sprites and wave-like text.
            MelonLogger.Msg("[Toolkit Global Icon Inventory] === active images ===");
            int imageCount = 0;
            foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (image == null || !image.gameObject.scene.IsValid() ||
                    !image.gameObject.activeInHierarchy || !image.enabled) continue;
                if (++imageCount > 1200) break;
                MelonLogger.Msg($"[Toolkit Global Icon Inventory] image path={GetPath(image.transform)} sprite={image.sprite?.name ?? "(none)"} color={image.color}");
            }
            MelonLogger.Msg($"[Toolkit Global Icon Inventory] images scanned={imageCount}");
            foreach (var text in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (text == null || !text.gameObject.scene.IsValid() ||
                    !text.gameObject.activeInHierarchy || !text.enabled) continue;
                string value = text.text ?? "";
                if (value.IndexOf("wave", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.Contains("/35") || value.Contains("/ 35"))
                    MelonLogger.Msg($"[Toolkit Global Icon Inventory] text path={GetPath(text.transform)} value={value}");
            }
            // Native building information panel: inspect structure, images and typography.
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid() ||
                    !rect.gameObject.activeInHierarchy || rect.name != "BuildingView(Clone)") continue;
                MelonLogger.Msg("[Toolkit Building Panel Detail] === building panel ===");
                foreach (var child in rect.GetComponentsInChildren<RectTransform>(true))
                {
                    if (child == null) continue;
                    var image = child.GetComponent<Image>();
                    var label = child.GetComponent<TextMeshProUGUI>();
                    if (image == null && label == null &&
                        child.name != "Panel" && child.name != "Header" && child.name != "Footer") continue;
                    MelonLogger.Msg($"[Toolkit Building Panel Detail] path={GetPath(child)} size={child.sizeDelta} pos={child.anchoredPosition} sprite={image?.sprite?.name ?? "(none)"} color={image?.color.ToString() ?? "(none)"} text={label?.text ?? ""} fontSize={label?.fontSize.ToString() ?? "(none)"}");
                }
            }
            // Name-independent scene inventory: list large, visible UI surfaces
            // and their immediate hierarchy, including their image sprites.
            int surfaces = 0;
            MelonLogger.Msg("[Toolkit Scene Surface Probe] === Large active UI surfaces ===");
            foreach (var rect in all)
            {
                if (rect == null || !rect.gameObject.scene.IsValid() || !rect.gameObject.activeInHierarchy) continue;
                if (rect.rect.width < 400f || rect.rect.height < 300f) continue;
                if (++surfaces > 140) break;
                MelonLogger.Msg($"[Toolkit Scene Surface Probe] #{surfaces} path={GetPath(rect)}");
                Describe(rect);
                for (int i = 0; i < rect.childCount && i < 18; i++)
                {
                    if (rect.GetChild(i) is not RectTransform child) continue;
                    Describe(child);
                }
            }
            MelonLogger.Msg($"[Toolkit HUD Probe] F8 finished: {matches} matching scene objects, {panels} named panels, {surfaces} large surfaces.");
        }
        catch (Exception exception) { MelonLogger.Error($"[Toolkit HUD Probe] {exception}"); }
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        for (int depth = 0; depth < 10 && transform.parent != null; depth++)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private static void Describe(RectTransform rect)
    {
        var image = rect.GetComponent<Image>();
        var parent = rect.parent;
        MelonLogger.Msg($"[Toolkit HUD Probe] {rect.name} parent={parent?.name ?? "(root)"} " +
            $"active={rect.gameObject.activeInHierarchy} pos={rect.anchoredPosition} size={rect.rect.size} " +
            $"anchor={rect.anchorMin}/{rect.anchorMax} sprite={image?.sprite?.name ?? "(none)"}");
    }
}

#endif
