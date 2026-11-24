using MelonLoader;
using Il2CppNvizzio.Game.Enums;
using Il2CppNvizzio.Game.GamePlay.Zones;
using Il2CppNvizzio.Game.GamePlay.Zones.Coordinates;
using Il2CppNvizzio.Game.Services;
using TowerDominion.Toolkit.Rng;

namespace TowerDominion.Toolkit.Game;

internal static class CorrectiveNeutralPlacement
{
    internal static NeutralPlacementOpportunity? Opportunity;
    internal static int? PositionX;
    internal static int? PositionY;
    internal static ZoneBasement? PlacementBasement;
    internal static int? IdentityIndex;
    internal static int? OrientationIndex;
    internal static TileBasement? CommittedTile;
    internal static GlobalizedTilePos? CommitWorldCell;
    internal static int PositionComponent;
    internal static bool IdentityReturnedToNative;
    internal static bool PositionNeedsReroll = true;
    internal static float? ExistenceRoll;
    [ThreadStatic] internal static int ExistenceNativeDepth;
    [ThreadStatic] internal static int NativeSpawnDepth;

    internal static bool IsActive =>
        Opportunity is not null &&
        ToolkitMod.CorrectiveRng?.Phase == CorrectiveRngLifecyclePhase.Attached;

    internal static void BeginOrRefresh(ZoneBasement basement)
    {
        CorrectiveRngLifecycle? lifecycle = ToolkitMod.CorrectiveRng;
        if (lifecycle is null || lifecycle.Phase == CorrectiveRngLifecyclePhase.Detached)
            return;

        var rules = ToolkitMod.SavedGameplayRules;
        bool fullDiscovery = rules?.IsEnabled("full-discovery") == true;
        bool noDiscovery = rules?.IsEnabled("no-discovery") == true;

        CorrectiveRngRunState? run = lifecycle.Current;
        bool exists;
        if (fullDiscovery)
        {
            run = lifecycle.ActivateCorrective();
            exists = true;
        }
        else if (noDiscovery)
        {
            exists = false;
        }
        else
        {
            run = lifecycle.ActivateCorrective();
            ExistenceRoll ??= (float)(run.Manager.Resolve(RngCatalog.Neutral.Existence).NextDouble() * 100.0);
            var manager = Il2CppNvizzio.Core.Singleton.MonoSingleton<ZoneManager>.Instance;
            exists = manager != null && manager.currentNeutralOdds > ExistenceRoll.Value;
        }

        if (!exists || run is null)
        {
            Opportunity = null;
            NeutralPreviewProbe.Pending = null;
            return;
        }

        var persistence = new CorrectiveRngPersistenceCoordinator(
            lifecycle, new FileCorrectiveRngPersistenceStore());
        CorrectiveRngPersistencePaths paths = CorrectiveRngGamePersistencePaths.Resolve();
        if (!persistence.IsCorrectiveRequired(paths))
            persistence.Activate(paths);

        Opportunity ??= new NeutralPlacementOpportunity(run.Manager);

        NeutralPositionDecision position;
        if (PositionNeedsReroll || Opportunity.CurrentPosition is not NeutralPositionDecision currentPosition)
        {
            position = Opportunity.RecomputePosition(
                (x, y) => NeutralPreviewProbe.IsIntrinsicLocalCellValid(basement, x, y));
            PositionNeedsReroll = false;
        }
        else
        {
            // Position/Y is intrinsic toolkit state. Translation and rotation only
            // change its projection and must never feed back into the RNG decision.
            position = currentPosition;
        }

        PositionX = position.XOffset;
        PositionY = position.YOffset;
        PlacementBasement = basement;

        List<string> eligible = BuildEligible();
        IdentityIndex = Opportunity.ExposeIdentity(eligible.Count);
        Zone? previewZone = NeutralPreviewProbe.CurrentPreview;
        if (previewZone is null)
            throw new InvalidOperationException("Corrective neutral preview zone is unavailable");
        GlobalizedTilePos previewGlobal = NeutralPreviewProbe.LocalPreviewToGlobal(
            previewZone, position.XOffset, position.YOffset);
        NeutralPreviewProbe.Pending = new(
            0,
            basement.Pointer,
            previewGlobal.X,
            previewGlobal.Y,
            eligible[IdentityIndex.Value],
            0);

        persistence.PersistAfterNativeSave(paths);
    }

    internal static void PrepareCommit()
    {
        if (!IsActive || Opportunity is null)
            return;

        List<string> eligible = BuildEligible();
        NeutralPlacementDecision decision = Opportunity.Commit(eligible.Count);
        IdentityIndex = decision.IdentityIndex;
        OrientationIndex = decision.OrientationIndex;
        PositionX = decision.Position.XOffset;
        PositionY = decision.Position.YOffset;

        if (PlacementBasement is null)
            throw new InvalidOperationException("Corrective neutral commit has no placement basement");

        PositionComponent = 0;
        IdentityReturnedToNative = false;
        CommittedTile = null;
        CommitWorldCell = null;
    }

    internal static void LatchCommitWorldCell()
    {
        if (!IsActive || PositionX is not int x || PositionY is not int y || PlacementBasement is null)
            return;

        Zone? preview = NeutralPreviewProbe.CurrentPreview;
        if (preview is null)
            throw new InvalidOperationException("Corrective neutral preview is unavailable at commit latch");

        CommitWorldCell = NeutralPreviewProbe.LocalPreviewToGlobal(preview, x, y);
        MelonLogger.Msg(
            $"[Toolkit RNG] Neutral commit cell latched local=({x},{y}) angle={preview.Angle:F0} global=({CommitWorldCell.X},{CommitWorldCell.Y}).");
    }

    internal static void CaptureCommittedTile(ZoneBasement basement)
    {
        if (!IsActive || PositionX is not int x || PositionY is not int y)
            return;

        Zone? zone = basement.Zone;
        if (zone is null)
            throw new InvalidOperationException("Corrective neutral committed basement has no zone");

        GlobalizedTilePos global = CommitWorldCell
            ?? throw new InvalidOperationException("Corrective neutral commit world cell was not latched before registration");
        var manager = Il2CppNvizzio.Core.Singleton.MonoSingleton<ZoneManager>.Instance;
        TileBasement? tile = manager?.GetTileBasementByGlobalizedTilePos(global);
        if (tile is null)
            throw new InvalidOperationException(
                $"Corrective neutral committed local tile ({x},{y}) did not resolve at global ({global.X},{global.Y})");
        if (!tile.IsFree || tile.TileHeight <= 0)
            throw new InvalidOperationException(
                $"Corrective neutral committed global tile ({global.X},{global.Y}) is invalid: free={tile.IsFree} height={tile.TileHeight}");

        CommittedTile = tile;
        MelonLogger.Msg(
            $"[Toolkit RNG] Neutral tile captured local=({x},{y}) angle={zone.Angle:F0} global=({global.X},{global.Y}) world=({tile.Transform.position.x:F2},{tile.Transform.position.y:F2},{tile.Transform.position.z:F2}).");
    }

    internal static TileBasement ResolveCommittedTile()
    {
        if (!IsActive || PositionX is not int x || PositionY is not int y)
            throw new InvalidOperationException("Corrective neutral position is incomplete at native spawn yield");

        if (CommittedTile is not null)
            return CommittedTile;

        if (PlacementBasement is null)
            throw new InvalidOperationException("Corrective neutral placement basement is unavailable");

        Zone? zone = PlacementBasement.Zone;
        if (zone is null)
            throw new InvalidOperationException("Corrective neutral placement basement has no committed zone");

        GlobalizedTilePos global = NeutralPreviewProbe.LocalPreviewToGlobal(zone, x, y);
        var manager = Il2CppNvizzio.Core.Singleton.MonoSingleton<ZoneManager>.Instance;
        TileBasement? tile = manager?.GetTileBasementByGlobalizedTilePos(global);
        if (tile is null)
            throw new InvalidOperationException(
                $"Corrective neutral local tile ({x},{y}) did not resolve at global ({global.X},{global.Y})");
        if (!tile.IsFree || tile.TileHeight <= 0)
            throw new InvalidOperationException(
                $"Corrective neutral global tile ({global.X},{global.Y}) is invalid: free={tile.IsFree} height={tile.TileHeight}");

        MelonLogger.Msg(
            $"[Toolkit RNG] Neutral tile resolve local=({x},{y}) angle={zone.Angle:F0} global=({global.X},{global.Y}) world=({tile.Transform.position.x:F2},{tile.Transform.position.y:F2},{tile.Transform.position.z:F2}).");
        return tile;
    }

    internal static void Complete()
    {
        Opportunity = null;
        PositionX = PositionY = IdentityIndex = OrientationIndex = null;
        PlacementBasement = null;
        CommittedTile = null;
        CommitWorldCell = null;
        PositionComponent = 0;
        IdentityReturnedToNative = false;
        ExistenceRoll = null;
        PositionNeedsReroll = true;
    }

    internal static void RefreshPersistenceIfNeeded()
    {
        CorrectiveRngLifecycle? lifecycle = ToolkitMod.CorrectiveRng;
        if (lifecycle is null || lifecycle.Phase != CorrectiveRngLifecyclePhase.Attached)
            return;

        try
        {
            var coordinator = new CorrectiveRngPersistenceCoordinator(
                lifecycle, new FileCorrectiveRngPersistenceStore());
            CorrectiveRngPersistencePaths paths = CorrectiveRngGamePersistencePaths.Resolve();
            if (!coordinator.IsCorrectiveRequired(paths))
                return;

            if (!File.Exists(paths.NativeSavePath) || !File.Exists(paths.SidecarPath))
                return;

            byte[] native = File.ReadAllBytes(paths.NativeSavePath);
            CorrectiveRngSidecar sidecar = CorrectiveRngSidecarCodec.Deserialize(File.ReadAllText(paths.SidecarPath));
            string currentHash = CorrectiveRngSidecarCodec.ComputeSha256(native);
            if (string.Equals(sidecar.NativeSaveSha256, currentHash, StringComparison.OrdinalIgnoreCase))
                return;

            coordinator.PersistAfterNativeSave(paths);
            MelonLogger.Msg("[Toolkit RNG] Corrective RNG sidecar refreshed for changed native Progress.gd generation.");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[Toolkit RNG] Corrective RNG persistence refresh failed: {ex}");
        }
    }

    internal static List<string> BuildEligible()
    {
        var eligible = new List<string>();
        DiscoveryNeutralSelectionScope.Depth++;
        try
        {
            foreach (var id in NeutralsType.Keys)
            {
                if (Services.UnlockablesService.IsBuildingUnlocked(id) &&
                    Services.UnlockablesService.IsBuildingAvailableInDemo(id))
                    eligible.Add(id);
            }
        }
        finally
        {
            DiscoveryNeutralSelectionScope.Depth--;
        }
        return eligible;
    }
}
