using Il2CppNvizzio.Game.GamePlay.Contexts;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>Shared rule state and scoped reroll logging.</summary>
internal static class BrytaRerollDiagnostics
{
    internal static bool Enabled => ToolkitMod.SavedGameplayRules?.IsEnabled("bryta-nest-reroll") == true;

    internal static string ZoneInfo(PlaceZoneContextData? data)
    {
        if (data?.ZoneToPlace == null) return "none";
        var zone = data.ZoneToPlace;
        return $"shape={zone.ZoneShape}, pattern={zone.GetSectionPatternName()}, id={zone.ZoneId}";
    }

    internal static void Report(string phase, string detail)
    {
        if (Enabled) MelonLogger.Msg($"[Toolkit Bryta probe] {phase}: {detail}");
    }
}
