namespace TowerDominion.Toolkit.Modules;

public static class DiscoveryRestrictions
{
    public static bool IsNeutralExcluded(GameplayRules? rules, string id) =>
        rules?.IsEnabled("full-discovery") == true && id == "NB15";

    public static bool IsDoctrineExcluded(GameplayRules? rules, string id)
    {
        bool none = rules?.IsEnabled("no-discovery") == true;
        bool full = rules?.IsEnabled("full-discovery") == true;
        return ((none || full) && id is ("D1_05" or "D1_37" or "D2_18" or "TBCB07D7"))
            || (none && id is ("D1_09" or "D1_23" or "D1_32"));
    }
}
