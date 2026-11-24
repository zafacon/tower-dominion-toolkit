namespace TowerDominion.Toolkit.Rng;

/// <summary>
/// Owns at most one corrective run. Game adapters may replace/clear the session at
/// native run boundaries; gameplay code never owns manager lifetime directly.
/// </summary>
public sealed class CorrectiveRngSession
{
    private readonly IRandomGeneratorFactory _factory;
    private readonly IRngKeyDeriver _deriver;

    public CorrectiveRngSession(IRandomGeneratorFactory factory, IRngKeyDeriver deriver)
    {
        _factory = factory;
        _deriver = deriver;
    }

    public CorrectiveRngRunState? Current { get; private set; }

    public bool IsAttached => Current is not null;

    public CorrectiveRngRunState StartNew(int nativeSeed)
    {
        ulong root = NativeRunSeed.ToRootSeed(nativeSeed);
        return Current = CorrectiveRngRun.Start(root, _factory, _deriver);
    }

    public CorrectiveRngRunState AttachLegacy(int nativeSeed)
    {
        ulong root = NativeRunSeed.ToRootSeed(nativeSeed);
        return Current = CorrectiveRngRun.AttachToExistingRun(root, _factory, _deriver);
    }

    public CorrectiveRngRunState Restore(int nativeSeed, RngSnapshot snapshot)
    {
        ulong root = NativeRunSeed.ToRootSeed(nativeSeed);
        var manager = new RngManager(root, _deriver, _factory);
        manager.RestoreSnapshot(snapshot);
        return Current = new(CorrectiveRngLoadMode.RestoredSnapshot, root, manager);
    }

    public void Clear() => Current = null;
}
