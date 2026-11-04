using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Resolves the locked run selection and evaluates player actions without executing them.</summary>
public sealed class ChallengeEnforcement
{
    private readonly ChallengeRegistry registry;
    private readonly ChallengeSelections selections;

    public ChallengeEnforcement(ChallengeRegistry registry, ChallengeSelections selections)
    {
        this.registry = registry;
        this.selections = selections;
    }

    /// <summary>Returns a veto if any active challenge rejects the proposed action.</summary>
    public ChallengeDecision Evaluate(ChallengeAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!selections.RunActive) return ChallengeDecision.Permit();

        List<IChallengeRule> rules = new();
        foreach (string id in selections.Active)
        {
            if (!registry.TryGet(id, out IChallengeRule? rule) || rule is null)
                return ChallengeDecision.Block($"Active challenge is unavailable: {id}");
            rules.Add(rule);
        }

        try
        {
            return ChallengeEvaluator.Evaluate(rules, action);
        }
        catch (Exception ex)
        {
            return ChallengeDecision.Block($"Challenge evaluation failed: {ex.GetType().Name}");
        }
    }
}
