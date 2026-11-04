using Il2CppNvizzio.Core.Singleton;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using Il2CppNvizzio.Game.GamePlay.Zones;
using Il2CppNvizzio.Game.GamePlay.Zones.Coordinates;
using TowerDominion.Toolkit.Modules;
using UnityEngine;
using Cell = TowerDominion.Toolkit.Modules.BuildingConnectivity.Cell;

namespace TowerDominion.Toolkit.Game;

/// <summary>Read-only, screen-projected expansion hints. Does not alter native adjacency effects.</summary>
internal sealed class ChainOfCommandFrontierOverlay
{
    private readonly List<Transform> markers = new();
    private float refreshAt;
    private float eligibleAt = -1f;
    private bool warned;
    private float diagnosticAt;
    private string refreshStatus = "not refreshed";
    private string stage = "starting";
    internal void Draw(bool menuOpen)
    {
        var selection = ToolkitMod.SavedChallenges;
        if (selection?.RunActive != true ||
            !selection.Active.Contains(ChainOfCommandRule.Id, StringComparer.OrdinalIgnoreCase))
        {
            markers.Clear();
            refreshAt = 0f;
            eligibleAt = -1f;
            diagnosticAt = 0f;
            warned = false;
            return;
        }
        // RunActive becomes true before the gameplay scene has finished transitioning.
        // Never inspect native map entities during the initial scene setup.
        if (eligibleAt < 0f) eligibleAt = Time.unscaledTime + 8f;
        if (Time.unscaledTime >= diagnosticAt)
        {
            diagnosticAt = Time.unscaledTime + 5f;
            MelonLoader.MelonLogger.Msg($"[ChainOfCommand overlay] active=true delay={Math.Max(0f, eligibleAt - Time.unscaledTime):F1}s menu={menuOpen} waveEnd={ChainOfCommandModalVisibility.WaveEnd} special={ChainOfCommandModalVisibility.SpecialReward} pause={ChainOfCommandModalVisibility.Pause} markers={markers.Count} status={refreshStatus}");
        }
        if (Time.unscaledTime < eligibleAt || menuOpen || ChainOfCommandModalVisibility.IsOpen) return;
        try
        {
            stage = "refresh";
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 0.5f;
                Refresh();
            }
            stage = "camera";
            var camera = Camera.main;
            if (camera == null) return;
            stage = "style";
            var labelStyle = GUI.skin.label;
            int oldFontSize = labelStyle.fontSize;
            labelStyle.fontSize = 24;
            stage = "draw";
            Color previous = GUI.color;
            try
            {
                GUI.color = new Color(1f, 0.88f, 0.12f, 1f);
                foreach (var marker in markers)
                {
                    if (marker == null) continue;
                    var point = camera.WorldToScreenPoint(marker.position);
                    if (point.z <= 0 || point.x < 0 || point.x > Screen.width ||
                        point.y < 0 || point.y > Screen.height) continue;
                    GUI.Label(new Rect(point.x - 14f, Screen.height - point.y - 16f, 36f, 36f), "+");
                }
            }
            finally { GUI.color = previous; labelStyle.fontSize = oldFontSize; }
        }
        catch (Exception exception)
        {
            markers.Clear();
            if (!warned)
            {
                warned = true;
                MelonLoader.MelonLogger.Warning("[ChainOfCommand frontier] stage=" + stage + " " + exception);
            }
        }
    }

    private void Refresh()
    {
        markers.Clear();
        var manager = MonoSingleton<EntityManager>.Instance;
        var zones = MonoSingleton<ZoneManager>.Instance;
        var hq = manager?.GetHQEntity();
        if (manager == null || zones == null || hq == null)
        {
            refreshStatus = $"native missing manager={manager == null} zones={zones == null} hq={hq == null}";
            return;
        }

        static HashSet<Cell> Footprint(BuildingEntity building)
        {
            var result = new HashSet<Cell>();
            var tiles = building.TileBasements;
            if (tiles != null)
                foreach (var tile in tiles)
                {
                    var pos = tile?.GlobalizedTilePos;
                    if (pos != null) result.Add(new Cell(pos.X, pos.Y));
                }
            return result;
        }

        var connected = Footprint(hq);
        if (connected.Count == 0) { refreshStatus = "HQ footprint empty"; return; }
        var remaining = new List<HashSet<Cell>>();
        foreach (var building in manager.Buildings)
        {
            if (building == null || building.Pointer == hq.Pointer || building.IsBuildingPreview) continue;
            var footprint = Footprint(building);
            if (footprint.Count > 0) remaining.Add(footprint);
        }
        static bool Touches(HashSet<Cell> footprint, HashSet<Cell> network)
        {
            foreach (var cell in footprint)
                if (network.Contains(new Cell(cell.X - 1, cell.Y)) ||
                    network.Contains(new Cell(cell.X + 1, cell.Y)) ||
                    network.Contains(new Cell(cell.X, cell.Y - 1)) ||
                    network.Contains(new Cell(cell.X, cell.Y + 1))) return true;
            return false;
        }
        bool expanded;
        do
        {
            expanded = false;
            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                if (!Touches(remaining[i], connected)) continue;
                connected.UnionWith(remaining[i]);
                remaining.RemoveAt(i);
                expanded = true;
            }
        } while (expanded);

        var frontier = new HashSet<Cell>();
        foreach (var cell in connected)
        {
            frontier.Add(new Cell(cell.X - 1, cell.Y));
            frontier.Add(new Cell(cell.X + 1, cell.Y));
            frontier.Add(new Cell(cell.X, cell.Y - 1));
            frontier.Add(new Cell(cell.X, cell.Y + 1));
        }
        foreach (var cell in frontier)
        {
            if (connected.Contains(cell)) continue;
            var tile = zones.GetTileBasementByGlobalizedTilePos(new GlobalizedTilePos(cell.X, cell.Y));
            if (tile != null && tile.IsFree && tile.Entity == null && tile.Transform != null)
                markers.Add(tile.Transform);
        }
        refreshStatus = $"HQ={connected.Count} frontier={frontier.Count} markers={markers.Count}";
    }
}
