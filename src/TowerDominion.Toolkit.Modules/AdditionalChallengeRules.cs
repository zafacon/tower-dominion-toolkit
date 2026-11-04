using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

public sealed class EightBallRule : IChallengeRule
{
    public const string Id = "eight-ball";
    public ChallengeDescriptor Descriptor { get; } = new(Id, "8-Ball",
        "Regular reinforcement drafts offer one bundle; special rewards are unchanged.",
        new HashSet<ChallengeActionKind>(), Author: "GameFamiliar");
    public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
}

public sealed class FrozenLogisticsRule : IChallengeRule
{
    public const string Id = "frozen-logistics";
    public ChallengeDescriptor Descriptor { get; } = new(Id, "Frozen Logistics",
        "Effective Supply and Recon production cannot exceed their starting values; reductions and purchases remain allowed.",
        new HashSet<ChallengeActionKind>());
    public ChallengeDecision Evaluate(ChallengeAction action) => ChallengeDecision.Permit();
}
