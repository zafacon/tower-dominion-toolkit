namespace TowerDominion.Toolkit.Model;

/// <summary>Player actions which may be restricted by challenges.</summary>
public enum ChallengeActionKind
{
    PlaceTower,
    SellBuilding,
    CancelBuilding,
    UpgradeBuilding,
    Reroll
}

/// <summary>Immutable description of an enforceable challenge.</summary>
public sealed record ChallengeDescriptor(
    string Id,
    string Name,
    string Description,
    IReadOnlySet<ChallengeActionKind> RestrictedActions,
    string? Author = null,
    IReadOnlyList<string>? KnownIncompatibilities = null);

/// <summary>Snapshot of a proposed player action, supplied by a game adapter.</summary>
public sealed record ChallengeAction(
    ChallengeActionKind Kind,
    string? EntityId = null,
    decimal? EffectiveCost = null,
    IReadOnlyDictionary<string, decimal>? AvailableTowerCosts = null,
    string? LeftmostBlueprintId = null);

/// <summary>Decision returned without changing game state.</summary>
public sealed record ChallengeDecision(bool Allowed, string? Reason = null)
{
    public static ChallengeDecision Permit() => new(true);
    public static ChallengeDecision Block(string reason) => new(false, reason);
}

/// <summary>A challenge evaluates actions but never modifies game state.</summary>
public interface IChallengeRule
{
    ChallengeDescriptor Descriptor { get; }
    ChallengeDecision Evaluate(ChallengeAction action);
}

/// <summary>Game-independent registry with unique challenge identifiers.</summary>
public sealed class ChallengeRegistry
{
    private readonly Dictionary<string, IChallengeRule> rules = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IChallengeRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (string.IsNullOrWhiteSpace(rule.Descriptor.Id))
            throw new ArgumentException("Challenge identifier is required.", nameof(rule));
        if (!rules.TryAdd(rule.Descriptor.Id, rule))
            throw new InvalidOperationException($"Challenge already registered: {rule.Descriptor.Id}");
    }

    public bool TryGet(string id, out IChallengeRule? rule) => rules.TryGetValue(id, out rule);
    public IReadOnlyCollection<ChallengeDescriptor> List() => rules.Values.Select(r => r.Descriptor).ToArray();
}

/// <summary>Combines challenge vetoes without executing the requested action.</summary>
public static class ChallengeEvaluator
{
    public static ChallengeDecision Evaluate(IEnumerable<IChallengeRule> active, ChallengeAction action)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(action);
        foreach (IChallengeRule rule in active)
        {
            if (!rule.Descriptor.RestrictedActions.Contains(action.Kind)) continue;
            ChallengeDecision decision = rule.Evaluate(action);
            if (!decision.Allowed) return decision;
        }
        return ChallengeDecision.Permit();
    }
}
