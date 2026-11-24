namespace TowerDominion.Toolkit.Rng;

public enum CorrectiveRngLoadMode
{
    NewRun,
    ExistingRunWithoutSnapshot,
    RestoredSnapshot
}

public sealed record CorrectiveRngRunState(
    CorrectiveRngLoadMode LoadMode,
    ulong RootSeed,
    RngManager Manager);

public static class CorrectiveRngRun
{
    public static CorrectiveRngRunState Start(
        ulong rootSeed,
        IRandomGeneratorFactory factory,
        IRngKeyDeriver deriver,
        string? persistedSnapshotJson = null)
    {
        var manager = new RngManager(rootSeed, deriver, factory);
        if (string.IsNullOrWhiteSpace(persistedSnapshotJson))
            return new(CorrectiveRngLoadMode.NewRun, rootSeed, manager);

        RngSnapshot snapshot = RngSnapshotJson.Deserialize(persistedSnapshotJson);
        manager.RestoreSnapshot(snapshot);
        return new(CorrectiveRngLoadMode.RestoredSnapshot, rootSeed, manager);
    }

    public static CorrectiveRngRunState AttachToExistingRun(
        ulong rootSeed,
        IRandomGeneratorFactory factory,
        IRngKeyDeriver deriver)
    {
        // Compatibility policy: an old save has no corrective history to reconstruct.
        // Start pristine semantic streams from the run root without consuming any of them.
        return new(
            CorrectiveRngLoadMode.ExistingRunWithoutSnapshot,
            rootSeed,
            new RngManager(rootSeed, deriver, factory));
    }

    public static string Save(CorrectiveRngRunState run) =>
        RngSnapshotJson.Serialize(run.Manager.CaptureSnapshot());
}
