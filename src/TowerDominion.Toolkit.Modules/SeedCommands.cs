using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

/// <summary>Interface-neutral command handler; transport and display are the caller's responsibility.</summary>
public sealed class SeedCommands
{
    private readonly SeedModule module;
    private readonly ISeedService seeds;

    public SeedCommands(SeedModule module, ISeedService seeds)
    {
        this.module = module ?? throw new ArgumentNullException(nameof(module));
        this.seeds = seeds ?? throw new ArgumentNullException(nameof(seeds));
    }

    public string Execute(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "Usage: seed set <value> | seed clear | seed status";
        }

        string[] parts = command.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 && parts[0] == "seed" && parts[1] == "set")
        {
            ModelOperation operation = module.RequestOverride(parts[2]);
            return $"{operation.Result.Status}: {operation.Result.Reason ?? parts[2]}";
        }

        if (parts.Length == 2 && parts[0] == "seed" && parts[1] == "clear")
        {
            return seeds.ClearOverride() ? "Pending seed override cleared." : "No pending override.";
        }

        if (parts.Length == 2 && parts[0] == "seed" && parts[1] == "status")
        {
            PropertySnapshot<string> snapshot = seeds.Seed.Snapshot;
            return $"Pending: {seeds.PendingOverride ?? "none"}; Observed: {snapshot.Observed ?? "unknown"}; Applied: {snapshot.Applied ?? "none"}; Verified: {snapshot.IsVerified}";
        }

        return "Usage: seed set <value> | seed clear | seed status";
    }
}
