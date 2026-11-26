using Il2CppNvizzio.Game.GamePlay.Blueprints;
using HarmonyLib;
using Il2CppNvizzio.Core.SaveManager;
using UnityEngine;
using TowerDominion.Toolkit.Modules;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

// Visual storage with run-specific metadata. Native inventory is never changed.
internal static class BlueprintBagPrototype
{
    private sealed record Hidden(BlueprintUI Card, CanvasGroup Group, float Alpha, bool Interactable, bool Raycasts);
    private static readonly Dictionary<string, Hidden> Stored = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] FeatureIds = { "blueprint-bag" };
    private static BlueprintBagPersistence? persistence;
    private static readonly HashSet<string> Pending = new(StringComparer.OrdinalIgnoreCase);
    private static bool runReady;
    internal static void Initialize(string directory)
    {
        try
        {
            persistence = new BlueprintBagPersistence(Path.Combine(directory, "qol-blueprint-bag.json"),
                Path.Combine(Application.persistentDataPath, "SaveFolder", "Progress.gd"));
        }
        catch (Exception ex) { MelonLogger.Warning($"[Blueprint Bag] Configuration unavailable: {ex.Message}"); }
    }
    private static bool Enabled => persistence?.Enabled == true;
    internal static void Begin(bool loaded)
    {
        Reset();
        try
        {
            if (loaded && Enabled) Pending.UnionWith(persistence!.Load());
            else if (!loaded) persistence?.Clear();
            runReady = true;
        }
        catch (Exception ex) { runReady = true; MelonLogger.Warning($"[Blueprint Bag] Restore unavailable: {ex.Message}"); }
    }
    internal static void Persist()
    {
        if (!runReady || persistence == null) return;
        try { persistence.Save(Enabled ? Stored.Keys.Concat(Pending) : Array.Empty<string>()); }
        catch (Exception ex) { MelonLogger.Warning($"[Blueprint Bag] Save failed: {ex.Message}"); }
    }
    internal static void Update()
    {
        if (Pending.Count == 0 || !Enabled) return;
        try
        {
            var hand = Hand;
            if (hand == null || hand.InputFrozen || hand.selectedBlueprint != null || hand.layout == null) return;
            if (!Compatible) { Pending.Clear(); Persist(); return; }
            var cards = Cards();
            foreach (var id in Pending.ToArray())
            {
                if (cards.ContainsKey(id)) Transfer(id, true);
                Pending.Remove(id);
            }
            Persist();
        }
        catch (Exception ex) { Pending.Clear(); MelonLogger.Warning($"[Blueprint Bag] Restore failed: {ex.Message}"); }
    }

    private static BlueprintHand? Hand => ToolkitMod.SavedChallenges?.RunActive == true
        && !ChallengeHudOverlay.TeardownStarted ? BlueprintHand.Instance : null;

    private static bool Compatible => ToolkitMod.SavedChallenges?.Active.Contains(
        ForcedDeploymentRule.Id, StringComparer.OrdinalIgnoreCase) != true;

    internal static BlueprintHand? VisibleHand => Enabled && Compatible ? Hand : null;
    internal sealed record DisplayCard(string Id, string Name, int Count, long Cost,
        UnityEngine.UI.Image? Art, UnityEngine.UI.Image? Background, UnityEngine.UI.Image? Badge,
        Il2CppTMPro.TMP_FontAsset? Font, UnityEngine.UI.Image? Mask, UnityEngine.UI.Image? Outline, UnityEngine.UI.Image? Shadow);
    internal static IReadOnlyList<DisplayCard> DisplayCards()
    {
        var cards = Cards();
        var result = new List<DisplayCard>();
        foreach (var id in Stored.Keys)
        {
            if (!cards.TryGetValue(id, out var card)) continue;
            string name = card.TowersType;
            try
            {
                var building = card.Data.GetBuildingData();
                if (building != null) name = building.BuildingNameLocString.GetLocalizedString();
            }
            catch (Exception) { /* Keep native tower code as fallback. */ }
            var quantity = card.countValue;
            var badge = quantity?.GetComponent<UnityEngine.UI.Image>() ?? quantity?.transform.parent?.GetComponent<UnityEngine.UI.Image>();
            result.Add(new DisplayCard(id, name, card.Count, card.Cost, card.illustrationImage,
                card.backgroundImage, badge, quantity?.font,
                card.transform.Find("BackgroundMask")?.GetComponent<UnityEngine.UI.Image>(),
                card.transform.Find("Frame")?.GetComponent<UnityEngine.UI.Image>(),
                card.transform.Find("Shadow")?.GetComponent<UnityEngine.UI.Image>()));
        }
        return result.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static Dictionary<string, BlueprintUI> Cards()
    {
        var result = new Dictionary<string, BlueprintUI>(StringComparer.OrdinalIgnoreCase);
        var hand = Hand;
        if (hand == null || hand.Blueprints == null) return result;
        foreach (var pair in hand.Blueprints)
            if (pair.Key != null && pair.Value != null && pair.Value.Count > 0)
                result[pair.Key.UniqueID] = pair.Value;
        return result;
    }

    internal static void Register(CommandGrammar grammar, CommandCatalogService catalog)
    {
        grammar.RegisterDomain(new("qol", "Quality of life features"));
        grammar.Register(new("list", "qol", "List quality of life features", Array.Empty<ArgumentDefinition>(),
            Handler: _ => CommandOutcome.Ok($"blueprint-bag: {(Enabled ? "enabled" : "disabled")}" +
                (Enabled && !Compatible ? " (inactive: Forced Deployment)" : ""))));
        foreach (bool enable in new[] { true, false })
            grammar.Register(new(enable ? "enable" : "disable", "qol", "Toggle quality of life feature",
                new[] { new ArgumentDefinition("id", "Feature ID", Completion: ArgumentCompletion.Literal,
                    AllowedValues: FeatureIds) }, Handler: _ => Execute(() =>
                {
                    if (persistence == null) throw new InvalidOperationException("QoL settings unavailable.");
                    if (enable && !Compatible) throw new InvalidOperationException("Incompatible with Forced Deployment.");
                    if (!enable)
                    {
                        foreach (var id in Stored.Keys.ToArray()) Transfer(id, false);
                        Pending.Clear();
                        persistence.Clear();
                    }
                    persistence.SetEnabled(enable);
                    return $"Blueprint Bag {(enable ? "enabled" : "disabled")}.";
                })));
        grammar.RegisterDomain(new("bag", "Blueprint visual storage"));
        foreach (bool store in new[] { true, false })
        {
            string[]? Candidates(bool namesOnly)
            {
                if (!Enabled || !Compatible) return Array.Empty<string>();
                var entries = catalog.List("blueprints");
                if (entries == null) return null;
                var cards = Cards();
                return entries.Where(e => cards.ContainsKey(e.Id) && Stored.ContainsKey(e.Id) != store)
                    .SelectMany(e => namesOnly ? new[] { e.Name } : new[] { e.Name, e.Id }.Concat(e.Aliases))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            grammar.Register(new(store ? "store" : "retrieve", "bag", "Transfer blueprint stack visually",
                new[] { new ArgumentDefinition("blueprint", "Blueprint name or ID", Completion: ArgumentCompletion.Catalog,
                    CatalogProvider: () => Candidates(false), CompletionProvider: () => Candidates(true)) },
                Handler: i => Execute(() =>
                {
                    var match = catalog.Resolve("blueprints", i.Arguments[0]);
                    if (match.Entity == null) throw new InvalidOperationException("Blueprint name or ID is unavailable or ambiguous.");
                    Transfer(match.Entity.Id, store);
                    Persist();
                    return $"{match.Entity.Name}: {(store ? "stored" : "retrieved")}.";
                })));
        }
        grammar.Register(new("list", "bag", "List stored blueprint stacks", Array.Empty<ArgumentDefinition>(),
            Handler: _ => Execute(() =>
            {
                var cards = Cards();
                var entries = catalog.List("blueprints");
                return Stored.Count == 0 ? "Bag empty." : string.Join("\n", Stored.Keys.Select(id =>
                    $"{entries?.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Name ?? id}: {(cards.TryGetValue(id, out var card) ? card.Count : 0)}"));
            })));
        grammar.Register(new("clear", "bag", "Restore all stored stacks", Array.Empty<ArgumentDefinition>(),
            Handler: _ => Execute(() =>
            {
                foreach (var id in Stored.Keys.ToArray()) Transfer(id, false);
                Pending.Clear();
                Persist();
                return "Bag cleared.";
            })));
    }

    private static CommandOutcome Execute(Func<string> action)
    {
        try { return CommandOutcome.Ok(action()); }
        catch (Exception ex) { return CommandOutcome.Failure(ex.Message, CommandErrorKind.Validation); }
    }

    internal static bool TryMouseTransfer(string id, bool store)
    {
        try
        {
            if (ToolkitConsoleOverlay.BlocksGameplayInput || VisibleHand == null) return false;
            Transfer(id, store);
            Persist();
            return true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[Blueprint Bag] Mouse transfer rejected: {ex.Message}");
            return false;
        }
    }

    internal static bool TryStoreToggle(Il2Cpp.StyledToggle toggle)
    {
        if (VisibleHand == null || ToolkitConsoleOverlay.BlocksGameplayInput) return false;
        foreach (var pair in Cards())
        {
            if (pair.Value.toggle != toggle || Stored.ContainsKey(pair.Key)) continue;
            return TryMouseTransfer(pair.Key, true);
        }
        return false;
    }

    private static void Transfer(string id, bool store)
    {
        var hand = Hand;
        if (hand == null || hand.layout == null) throw new InvalidOperationException("No active blueprint hand.");
        if (hand.InputFrozen) throw new InvalidOperationException("Blueprint hand is frozen.");
        if (!Enabled && store) throw new InvalidOperationException("Enable blueprint-bag first.");
        if (!Compatible && store) throw new InvalidOperationException("Incompatible with Forced Deployment.");
        if (hand.selectedBlueprint != null) throw new InvalidOperationException("Cancel current blueprint selection first.");
        var cards = Cards();
        if (!cards.TryGetValue(id, out var card)) throw new InvalidOperationException("Blueprint is not in the native hand.");
        var layout = hand.layout;
        var animation = card.BlueprintAnimation;
        if (animation == null) throw new InvalidOperationException("Card animation unavailable.");
        if (store)
        {
            if (Stored.ContainsKey(id)) return;
            var group = card.GetComponent<CanvasGroup>() ?? card.gameObject.AddComponent<CanvasGroup>();
            if (group == null) throw new InvalidOperationException("CanvasGroup unavailable.");
            var state = new Hidden(card, group, group.alpha, group.interactable, group.blocksRaycasts);
            Stored.Add(id, state);
            try
            {
                group.alpha = 0;
                group.interactable = false;
                group.blocksRaycasts = false;
                layout.OnBlueprintRemovedFromHand(animation);
            }
            catch
            {
                RestoreVisual(state);
                Stored.Remove(id);
                if (!layout._blueprintAnimations.Contains(animation)) layout.OnBlueprintAddedToHand(animation);
                throw;
            }
        }
        else
        {
            if (!Stored.TryGetValue(id, out var state)) return;
            if (!layout._blueprintAnimations.Contains(animation)) layout.OnBlueprintAddedToHand(animation);
            RestoreVisual(state);
            Stored.Remove(id);
        }
        MelonLogger.Msg($"[Blueprint Bag] {id} stored={store} count={card.Count} native={cards.Count} layout={layout._blueprintAnimations.Count}");
    }

    private static void RestoreVisual(Hidden state)
    {
        if (state.Group == null) return;
        state.Group.alpha = state.Alpha;
        state.Group.interactable = state.Interactable;
        state.Group.blocksRaycasts = state.Raycasts;
    }

    internal static void Reset() { Stored.Clear(); Pending.Clear(); runReady = false; }
}

// Forget scene-owned wrappers at native hand destruction/reset.
[HarmonyPatch(typeof(BlueprintHand), nameof(BlueprintHand.Clear))]
internal static class BlueprintBagClearPatch
{
    private static void Postfix() => BlueprintBagPrototype.Reset();
}

[HarmonyPatch(typeof(BlueprintHand), nameof(BlueprintHand.InitializeStartingBlueprints))]
internal static class BlueprintBagNewRunPatch
{
    private static void Postfix(BlueprintHand __instance)
    {
        // Retry reuses MainLoopContext and its active challenge snapshot.
        // Native starting-hand initialization occurs after progress cleanup.
        if (ToolkitMod.SavedChallenges?.RunActive == true && ChallengeHudOverlay.TeardownStarted)
        {
            ChallengeHudOverlay.TeardownStarted = false;
            ChainOfCommandModalVisibility.Reset();
            MelonLogger.Msg("[Toolkit] Gameplay HUD lifetime renewed after starting-hand initialization.");
        }
        BlueprintBagPanel.RemovePreviousControls(__instance.layout?.transform);
        BlueprintBagPrototype.Begin(false);
    }
}

[HarmonyPatch(typeof(BlueprintHand), nameof(BlueprintHand.LoadSavedBlueprints))]
internal static class BlueprintBagLoadPatch
{
    private static void Postfix(BlueprintHand __instance)
    {
        BlueprintBagPanel.RemovePreviousControls(__instance.layout?.transform);
        BlueprintBagPrototype.Begin(true);
    }
}

[HarmonyPatch(typeof(SaveSystem), "SaveToStorage", typeof(string), typeof(string))]
internal static class BlueprintBagSavePatch
{
    private static void Postfix(string __1)
    {
        if (string.Equals(__1, "Progress.gd", StringComparison.OrdinalIgnoreCase)) BlueprintBagPrototype.Persist();
    }
}
