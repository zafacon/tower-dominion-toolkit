namespace TowerDominion.Toolkit.Rng;

/// <summary>
/// Run-scoped decision cache for one neutral placement opportunity.
/// Identity and orientation are materialized at most once. Position is intentionally
/// re-evaluatable because placement geometry may change while the opportunity remains live.
/// </summary>
public sealed class NeutralPlacementOpportunity
{
    private readonly RngStream _position;
    private readonly RngStream _identity;
    private readonly RngManager _manager;
    private int? _identityIndex;
    private int? _orientationIndex;
    private NeutralPositionDecision? _positionDecision;
    private bool _committed;
    private bool _cancelled;

    public NeutralPlacementOpportunity(RngManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        _manager = manager;
        _position = manager.Resolve(RngCatalog.Neutral.Position);
        _identity = manager.Resolve(RngCatalog.Neutral.Identity);
    }

    public bool IsCommitted => _committed;
    public bool IsCancelled => _cancelled;
    public int? ExposedIdentityIndex => _identityIndex;
    public NeutralPositionDecision? CurrentPosition => _positionDecision;

    public int ExposeIdentity(int eligibleCount)
    {
        RequireLive();
        if (eligibleCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(eligibleCount));

        if (_identityIndex is int existing)
        {
            if (existing >= eligibleCount)
                throw new InvalidOperationException(
                    "The eligible neutral set changed incompatibly after identity was exposed.");
            return existing;
        }

        return (_identityIndex = _identity.NextInt32(0, eligibleCount)).Value;
    }

    public NeutralPositionDecision RecomputePosition(
        Func<int, int, bool> isValid,
        int minimumOffset = -2,
        int maximumOffsetExclusive = 3,
        int maximumAttempts = 1024)
    {
        RequireLive();
        ArgumentNullException.ThrowIfNull(isValid);
        if (minimumOffset >= maximumOffsetExclusive)
            throw new ArgumentOutOfRangeException(nameof(maximumOffsetExclusive));
        if (maximumAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));

        for (int attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            int x = _position.NextInt32(minimumOffset, maximumOffsetExclusive);
            int y = _position.NextInt32(minimumOffset, maximumOffsetExclusive);
            if (isValid(x, y))
                return _positionDecision = new(x, y, attempt);
        }

        throw new InvalidOperationException(
            $"No valid neutral position found within {maximumAttempts} attempts.");
    }

    public NeutralPlacementDecision Commit(int eligibleCount, int orientationCount = 4)
    {
        RequireLive();
        if (_positionDecision is not NeutralPositionDecision position)
            throw new InvalidOperationException("Neutral position must be resolved before commit.");
        if (orientationCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(orientationCount));

        int identity = ExposeIdentity(eligibleCount);
        int orientation = _orientationIndex ??=
            _manager.Resolve(RngCatalog.Neutral.Orientation).NextInt32(0, orientationCount);
        _committed = true;
        return new(identity, position, orientation);
    }

    public void Cancel()
    {
        RequireLive();
        _cancelled = true;
    }

    private void RequireLive()
    {
        if (_committed)
            throw new InvalidOperationException("Neutral placement opportunity is already committed.");
        if (_cancelled)
            throw new InvalidOperationException("Neutral placement opportunity is cancelled.");
    }
}

public sealed record NeutralPositionDecision(int XOffset, int YOffset, int Attempts);

public sealed record NeutralPlacementDecision(
    int IdentityIndex,
    NeutralPositionDecision Position,
    int OrientationIndex);
