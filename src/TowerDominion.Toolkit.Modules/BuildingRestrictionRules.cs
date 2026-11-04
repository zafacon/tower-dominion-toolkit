using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Prevents player-initiated removal of buildings while Chain of Command is active.</summary>
public sealed class ChainOfCommandRule : IChallengeRule
{
    public const string Id = "chain-of-command";
    public ChallengeDescriptor Descriptor { get; } = new(Id, "Chain of Command",
        "Buildings must extend the HQ-connected network.",
        new HashSet<ChallengeActionKind> { ChallengeActionKind.SellBuilding, ChallengeActionKind.CancelBuilding },
        Author: "DiVerrencia");

    public ChallengeDecision Evaluate(ChallengeAction action) => action.Kind switch
    {
        ChallengeActionKind.SellBuilding or ChallengeActionKind.CancelBuilding =>
            ChallengeDecision.Block("Chain of Command prevents removing buildings."),
        _ => ChallengeDecision.Permit()
    };
}

/// <summary>Restricts upgrades to the left branch; native adapter supplies a verified branch identifier.</summary>
public sealed class LeftTurnOnlyRule : IChallengeRule
{
    public const string Id = "left-turn-only";
    public ChallengeDescriptor Descriptor { get; } = new(Id, "Left Turn Only",
        "Only the left building upgrade path is available.",
        new HashSet<ChallengeActionKind> { ChallengeActionKind.UpgradeBuilding },
        Author: "bigbossodin");

    public ChallengeDecision Evaluate(ChallengeAction action) =>
        action.Kind != ChallengeActionKind.UpgradeBuilding ||
        string.Equals(action.EntityId, "left", StringComparison.OrdinalIgnoreCase)
            ? ChallengeDecision.Permit()
            : ChallengeDecision.Block("Only the left upgrade path is available.");
}
