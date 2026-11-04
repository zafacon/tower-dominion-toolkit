using Il2CppNvizzio.Core.ManagedData;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Blueprints;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Characters;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Doctrines;
using Il2CppNvizzio.Game.GamePlay.DataStructure.Settings;
using MelonLoader;
using TowerDominion.Toolkit.Model;
using TowerDominion.Toolkit.Modules;

namespace TowerDominion.Toolkit.Game;

/// <summary>Reads game-owned managed data into game-neutral catalog snapshots.</summary>
internal sealed class GameCatalogAdapter : ICommandCatalogSource
{
    private readonly EntityCatalog catalog;
    private bool ready;

    public GameCatalogAdapter(EntityCatalog catalog) => this.catalog = catalog;

    public bool IsReady => ready;

    public IReadOnlyList<CatalogEntity> List(string domain) => catalog.List(domain);

    public void RefreshWhenReady()
    {
        if (ready) return;
        try
        {
            var resources = ManagedDataHandler.DataResources;
            if (resources?.SubDataObjects == null || resources.SubDataObjects.Count == 0) return;

            // The native ban patch filters threat-level swarm candidates, not all enemy characters.
            var regularWaveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var finalWaveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var data in resources.SubDataObjects)
            {
                if (data?.TryCast<GeneralSettingsData>() is not { } settings) continue;
                foreach (var threat in settings.ThreatLevels)
                {
                    if (threat == null) continue;
                    var target = threat.IsFinalWave ? finalWaveIds : regularWaveIds;
                    foreach (var swarm in threat.EnemySwarms)
                        if (swarm != null && !string.IsNullOrWhiteSpace(swarm.CharacterId)) target.Add(swarm.CharacterId);
                }
            }

            var resolvedRegular = ResolveSwarmIds(regularWaveIds);
            var resolvedFinal = ResolveSwarmIds(finalWaveIds);

            var groups = new Dictionary<string, Dictionary<string, CatalogEntity>>(StringComparer.OrdinalIgnoreCase);
            foreach (string category in new[] { "foes", "waves", "blueprints", "doctrines", "frontiers" })
                groups[category] = new(StringComparer.OrdinalIgnoreCase);

            static void Add(Dictionary<string, Dictionary<string, CatalogEntity>> groups, string category, string? id, string? name, IReadOnlyCollection<string>? aliases = null)
            {
                if (string.IsNullOrWhiteSpace(id)) return;
                groups[category].TryAdd(id, new CatalogEntity(id, string.IsNullOrWhiteSpace(name) ? id : name, aliases ?? Array.Empty<string>()));
            }

            foreach (var data in resources.SubDataObjects)
            {
                if (data == null) continue;
                if (data.TryCast<CharacterData>() is { } character)
                {
                    var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { character.UniqueID };
                    if (character.Prefab != null)
                    {
                        if (!string.IsNullOrWhiteSpace(character.Prefab.name)) aliases.Add(character.Prefab.name);
                        if (character.Prefab.gameObject != null && !string.IsNullOrWhiteSpace(character.Prefab.gameObject.name)) aliases.Add(character.Prefab.gameObject.name);
                    }
                    if (character.CharacterIcon != null && !string.IsNullOrWhiteSpace(character.CharacterIcon.name)) aliases.Add(character.CharacterIcon.name);
                    string name = Localize(character.CharacterNameLocString, character.UniqueID);
                    // ResourceLoadEnemy is also used by SwarmBanPatch, so the catalog resolves
                    // precisely the same short character IDs as native swarm selection.
                    foreach (var (swarmId, selectedId) in resolvedRegular)
                    {
                        if (selectedId.Equals(character.UniqueID, StringComparison.OrdinalIgnoreCase))
                            Add(groups, "foes", character.UniqueID, name, aliases.Append(swarmId).ToArray());
                    }
                    foreach (var (swarmId, selectedId) in resolvedFinal)
                    {
                        if (selectedId.Equals(character.UniqueID, StringComparison.OrdinalIgnoreCase))
                            Add(groups, "waves", character.UniqueID, name, aliases.Append(swarmId).ToArray());
                    }
                }
                else if (data.TryCast<BlueprintData>() is { } blueprint)
                {
                    string name = blueprint.UniqueID;
                    try
                    {
                        var building = blueprint.GetBuildingData();
                        if (building != null) name = Localize(building.BuildingNameLocString, blueprint.TowersType);
                    }
                    catch (Exception exception) { MelonLogger.Warning($"[Toolkit] Blueprint label fallback: {exception.Message}"); }
                    Add(groups, "blueprints", blueprint.UniqueID, name);
                }
                else if (data.TryCast<SkillData>() is { } skill)
                {
                    string name = Localize(skill.TitleLocString, skill.UniqueID);
                    Add(groups, "doctrines", skill.UniqueID, name);
                    if (skill.SkillType == SkillType.Regular && skill.ExpertCost > 0)
                        Add(groups, "frontiers", skill.UniqueID, name);
                }
            }

            foreach (var group in groups)
                catalog.Replace(group.Key, group.Value.Values);
            ready = true;
            foreach (string name in new[] { "Mission control", "Scout Hideout" })
            {
                if (catalog.TryResolve("blueprints", name, out var resolved) && resolved != null)
                    MelonLogger.Msg($"[Toolkit Challenges] Blueprint target {name}: {resolved.Id}");
                else
                {
                    MelonLogger.Warning($"[Toolkit Challenges] Blueprint target unresolved: {name}; challenge acquisition ban requires verification.");
                    foreach (var candidate in catalog.List("blueprints").Where(x => x.Name.Contains("Mission", StringComparison.OrdinalIgnoreCase) || x.Name.Contains("Control", StringComparison.OrdinalIgnoreCase)))
                        MelonLogger.Msg($"[Toolkit Challenges] Candidate blueprint: {candidate.Name} ({candidate.Id})");
                }
            }
            MelonLogger.Msg("[Toolkit] Catalog loaded: " + string.Join(", ", groups.Select(x => $"{x.Key}={x.Value.Count}")));
        }
        catch (Exception exception)
        {
            MelonLogger.Warning($"[Toolkit] Catalog load deferred: {exception.Message}");
        }
    }

    private static Dictionary<string, string> ResolveSwarmIds(IEnumerable<string> swarmIds)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string id in swarmIds)
        {
            try
            {
                CharacterData? character = CharacterData.ResourceLoadEnemy(id);
                if (character != null && !string.IsNullOrWhiteSpace(character.UniqueID))
                    result[id] = character.UniqueID;
                else MelonLogger.Warning($"[Toolkit Catalog] Unresolved swarm character: {id}");
            }
            catch (Exception exception)
            {
                MelonLogger.Warning($"[Toolkit Catalog] Cannot resolve swarm {id}: {exception.Message}");
            }
        }
        return result;
    }

    private static string Localize(UnityEngine.Localization.LocalizedString? value, string fallback)
    {
        try
        {
            string? label = value?.GetLocalizedString();
            return string.IsNullOrWhiteSpace(label) ? fallback.Replace('_', ' ') : label;
        }
        catch { return fallback.Replace('_', ' '); }
    }
}
