namespace TowerDominion.Toolkit.Rng;

public enum CorrectiveRngLifecyclePhase
{
    Detached,
    RootObserved,
    Attached
}

/// <summary>
/// Two-phase run attachment. Native start establishes run identity first; persistence
/// validation then decides whether the run is legacy, restored, or unsafe to attach.
/// </summary>
public sealed class CorrectiveRngLifecycle
{
    private readonly CorrectiveRngSession _session;

    public CorrectiveRngLifecycle(CorrectiveRngSession session)
    {
        _session = session;
    }

    public CorrectiveRngLifecyclePhase Phase { get; private set; }

    public string? EncodedGameId { get; private set; }

    public int? NativeSeed { get; private set; }

    public CorrectiveRngRunState? Current => _session.Current;

    public void ObserveRoot(string encodedGameId, int nativeSeed)
    {
        if (string.IsNullOrWhiteSpace(encodedGameId))
            throw new ArgumentException("Encoded GAME ID cannot be null or whitespace.", nameof(encodedGameId));

        _session.Clear();
        EncodedGameId = encodedGameId;
        NativeSeed = nativeSeed;
        Phase = CorrectiveRngLifecyclePhase.RootObserved;
    }

    public CorrectiveRngRunState AttachLegacy()
    {
        int seed = RequireObservedSeed();
        CorrectiveRngRunState run = _session.AttachLegacy(seed);
        Phase = CorrectiveRngLifecyclePhase.Attached;
        return run;
    }

    public CorrectiveRngRunState StartCorrective()
    {
        int seed = RequireObservedSeed();
        CorrectiveRngRunState run = _session.StartNew(seed);
        Phase = CorrectiveRngLifecyclePhase.Attached;
        return run;
    }

    /// <summary>
    /// Promotes the current run to corrective ownership without ever replacing an
    /// already-attached manager. A fresh run receives its first pristine manager here;
    /// a loaded legacy run keeps the manager attached during load.
    /// </summary>
    public CorrectiveRngRunState ActivateCorrective()
    {
        if (Phase == CorrectiveRngLifecyclePhase.Attached && Current is not null)
            return Current;

        return StartCorrective();
    }

    public CorrectiveRngRunState Restore(RngSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        int seed = RequireObservedSeed();
        CorrectiveRngRunState run = _session.Restore(seed, snapshot);
        Phase = CorrectiveRngLifecyclePhase.Attached;
        return run;
    }

    public void Clear()
    {
        _session.Clear();
        EncodedGameId = null;
        NativeSeed = null;
        Phase = CorrectiveRngLifecyclePhase.Detached;
    }

    private int RequireObservedSeed() =>
        NativeSeed ?? throw new InvalidOperationException("A native run root must be observed before corrective RNG can attach.");
}
