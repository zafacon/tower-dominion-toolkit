namespace TowerDominion.Toolkit.Rng;

/// <summary>
/// Stable semantic keys. Values in this catalog are persistence identifiers:
/// do not rename or repurpose after release without an explicit migration.
/// </summary>
public static class RngCatalog
{
    public static class Map
    {
        public static readonly RngKey ZoneSelection = new("Run", "Map", "ZoneSelection");
        public static readonly RngKey ZoneOrientation = new("Run", "Map", "ZoneOrientation");
        public static readonly RngKey NestSelection = new("Run", "Map", "NestSelection");
        public static readonly RngKey DeadEndSelection = new("Run", "Map", "DeadEndSelection");
        public static readonly RngKey DeadEndOrientation = new("Run", "Map", "DeadEndOrientation");
        public static readonly RngKey HqSelection = new("Run", "Map", "HQSelection");
        public static readonly RngKey HqOrientation = new("Run", "Map", "HQOrientation");
        public static readonly RngKey UnchartedSelection = new("Run", "Map", "UnchartedSelection");
        public static readonly RngKey UnchartedOrientation = new("Run", "Map", "UnchartedOrientation");
    }

    public static class Neutral
    {
        public static readonly RngKey Existence = new("Run", "Neutral", "Existence");
        public static readonly RngKey Position = new("Run", "Neutral", "Position");
        public static readonly RngKey Identity = new("Run", "Neutral", "Identity");
        public static readonly RngKey Orientation = new("Run", "Neutral", "Orientation");
    }

    public static class Reward
    {
        public static readonly RngKey Doctrine = new("Run", "Reward", "Doctrine");
        public static readonly RngKey Blueprint = new("Run", "Reward", "Blueprint");
        public static readonly RngKey SpecialReward = new("Run", "Reward", "SpecialReward");
        public static readonly RngKey Bundle = new("Run", "Reward", "Bundle");
    }

    public static class Wave
    {
        public static readonly RngKey ThreatLevelSelection = new("Run", "Wave", "ThreatLevelSelection");
        public static readonly RngKey SwarmSelection = new("Run", "Wave", "SwarmSelection");
        public static readonly RngKey PredeterminedSwarm = new("Run", "Wave", "PredeterminedSwarm");
        public static readonly RngKey SpawnPath = new("Run", "Wave", "SpawnPath");
        public static readonly RngKey SpawnTiming = new("Run", "Wave", "SpawnTiming");
        public static readonly RngKey UnitSpawnTiming = new("Run", "Wave", "UnitSpawnTiming");
    }

    public static class Combat
    {
        public static readonly RngKey PathSelection = new("Run", "Combat", "PathSelection");
        public static readonly RngKey TargetSelection = new("Run", "Combat", "TargetSelection");
        public static readonly RngKey PositionSelection = new("Run", "Combat", "PositionSelection");
        public static readonly RngKey Timing = new("Run", "Combat", "Timing");
        public static readonly RngKey Variation = new("Run", "Combat", "Variation");
    }

    public static IReadOnlyList<RngKey> StableRunKeys { get; } = new[]
    {
        Map.ZoneSelection, Map.ZoneOrientation, Map.NestSelection, Map.DeadEndSelection,
        Map.DeadEndOrientation, Map.HqSelection, Map.HqOrientation, Map.UnchartedSelection,
        Map.UnchartedOrientation,
        Neutral.Existence, Neutral.Position, Neutral.Identity, Neutral.Orientation,
        Reward.Doctrine, Reward.Blueprint, Reward.SpecialReward, Reward.Bundle,
        Wave.ThreatLevelSelection, Wave.SwarmSelection, Wave.PredeterminedSwarm,
        Wave.SpawnPath, Wave.SpawnTiming, Wave.UnitSpawnTiming,
        Combat.PathSelection, Combat.TargetSelection, Combat.PositionSelection,
        Combat.Timing, Combat.Variation
    };
}
