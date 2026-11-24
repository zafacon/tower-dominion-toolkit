using HarmonyLib;
using Il2CppNvizzio.Game.GamePlay.GameEntities;
using Il2CppNvizzio.Game.Services.Threats;
using MelonLoader;

namespace TowerDominion.Toolkit.Game;

/// <summary>Opt-in observational counters; never changes native gameplay or awards Honors.</summary>
internal static class HonorsDiagnostics
{
    private static readonly Dictionary<string, int> Kills = new(StringComparer.Ordinal);
    private static readonly HashSet<nint> SeenDeaths = new();
    internal static bool Enabled { get; private set; }
    private static int waveStarts;
    private static int waveEnds;
    private static int removed;
    private static int deaths;
    private static int armoredDeaths;
    private static int aircraftDeaths;
    private static int dividingDeaths;
    private static int tunnelerDeaths;
    private static int camouflageDeaths;
    private static int missingSources;

    internal static void Initialize(string toolkitDirectory)
    {
        Enabled = File.Exists(Path.Combine(toolkitDirectory, "honors-diagnostics.enabled"));
        if (Enabled) MelonLogger.Msg("[Toolkit Honors] Read-only diagnostics enabled for this session.");
    }

    internal static void NewGame(GameRoundService service)
    {
        if (!Enabled) return;
        Summary("previous-run");
        Kills.Clear(); SeenDeaths.Clear(); waveStarts = waveEnds = removed = deaths = 0;
        armoredDeaths = aircraftDeaths = dividingDeaths = tunnelerDeaths = camouflageDeaths = missingSources = 0;
        Report($"new-game commander={service.CurrentHeroType} faction={service.CurrentFactionType} difficulty={service.RunData?.DifficultyValue} round={service.RoundIndex}");
    }

    internal static void WaveStart(GameRoundService service)
    {
        if (!Enabled) return;
        waveStarts++;
        Report($"wave-start roundIndex={service.RoundIndex} endless={service.IsEndlessMode} starts={waveStarts}");
    }

    internal static void WaveEnd(GameRoundService service)
    {
        if (!Enabled) return;
        waveEnds++;
        Summary($"wave-end roundIndex={service.RoundIndex} endless={service.IsEndlessMode}");
    }

    internal static void Death(CharacterEntity entity, GameEntity source)
    {
        if (!Enabled || entity == null || !entity.IsEnemy) return;
        nint id = entity.Pointer;
        if (!SeenDeaths.Add(id)) return;
        deaths++;
        if (entity.IsArmor) armoredDeaths++;
        if (entity.IsAircraft) aircraftDeaths++;
        if (entity.CharacterData?.IsDividing == true) dividingDeaths++;
        if (entity.CharacterData?.IsTunneler == true) tunnelerDeaths++;
        if (entity.IsCamouflage) camouflageDeaths++;
        if (source == null) missingSources++;
        string type = entity.CharacterData?.UniqueID ?? entity.GetEntityType() ?? "unknown";
        Kills[type] = Kills.TryGetValue(type, out int count) ? count + 1 : 1;
    }

    internal static void Removed(CharacterEntity entity)
    {
        if (!Enabled || entity == null || !entity.IsEnemy) return;
        removed++;
        SeenDeaths.Remove(entity.Pointer);
    }

    internal static void Summary(string phase)
    {
        if (!Enabled) return;
        Report($"{phase} deaths={deaths} removed={removed} starts={waveStarts} ends={waveEnds} armored={armoredDeaths} aircraft={aircraftDeaths} dividing={dividingDeaths} tunneler={tunnelerDeaths} camouflage={camouflageDeaths} noSource={missingSources} types=[{string.Join(", ", Kills.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"))}]");
    }

    private static void Report(string message) => MelonLogger.Msg($"[Toolkit Honors] {message}");
}

[HarmonyPatch(typeof(GameRoundService), nameof(GameRoundService.StartNewGame))]
internal static class HonorsNewGameProbe
{
    private static void Postfix(GameRoundService __instance) => HonorsDiagnostics.NewGame(__instance);
}

[HarmonyPatch(typeof(GameRoundService), nameof(GameRoundService.StartWave))]
internal static class HonorsWaveStartProbe
{
    private static void Postfix(GameRoundService __instance) => HonorsDiagnostics.WaveStart(__instance);
}

[HarmonyPatch(typeof(GameRoundService), nameof(GameRoundService.EndWave))]
internal static class HonorsWaveEndProbe
{
    private static void Postfix(GameRoundService __instance) => HonorsDiagnostics.WaveEnd(__instance);
}

[HarmonyPatch(typeof(GameRoundService), nameof(GameRoundService.GameOver))]
internal static class HonorsGameOverProbe
{
    private static void Prefix() => HonorsDiagnostics.Summary("game-over");
}

[HarmonyPatch(typeof(CharacterEntity), "Dead")]
internal static class HonorsDeathProbe
{
    private static void Prefix(CharacterEntity __instance, GameEntity sourceEntity) => HonorsDiagnostics.Death(__instance, sourceEntity);
}
