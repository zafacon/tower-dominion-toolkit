using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Modules;

public static class ChallengeCommandRegistration
{
    public static void Register(CommandGrammar grammar, ChallengeRegistry registry, ChallengeSelections selections)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(selections);
        if (!grammar.Domains.Any(d => d.Name.Equals("challenges", StringComparison.OrdinalIgnoreCase)))
            grammar.RegisterDomain(new("challenges", "Challenge selection"));
        grammar.Register(new("list", "challenges", "List challenges", Array.Empty<ArgumentDefinition>(),
            Handler: _ => ListChallenges(registry, registry.List().Select(x => x.Id).ToArray())));
        grammar.Register(new("list", "challenges", "List selected challenges", Array.Empty<ArgumentDefinition>(),
            Qualifier: "selected", Handler: _ => ListChallenges(registry, selections.Selected.ToArray())));
        grammar.Register(new("list", "challenges", "List active challenges", Array.Empty<ArgumentDefinition>(),
            Qualifier: "active", Handler: _ => ListChallenges(registry, selections.Active.ToArray())));
        grammar.Register(new("show", "challenges", "Show challenge details",
            new[] { new ArgumentDefinition("id", "Challenge ID",
                Completion: ArgumentCompletion.Catalog,
                CatalogProvider: () => registry.List().Select(x => x.Id).ToArray()) },
            Handler: invocation =>
            {
                string id = invocation.Arguments[0];
                if (!registry.TryGet(id, out var rule) || rule == null)
                    return CommandOutcome.Failure($"Unknown challenge: {id}", CommandErrorKind.Validation);
                var d = rule.Descriptor;
                string incompatibilities = d.KnownIncompatibilities is { Count: > 0 }
                    ? string.Join(", ", d.KnownIncompatibilities.Select(otherId =>
                        registry.TryGet(otherId, out var other) && other != null ? other.Descriptor.Name : otherId))
                    : "None known";
                return CommandOutcome.Ok($"{d.Name}\nID: {d.Id}\nAuthor: {d.Author ?? "Unattributed"}\n{d.Description}\nKnown incompatibilities: {incompatibilities}");
            }));
        grammar.Register(new("clear", "challenges", "Clear selected challenges", Array.Empty<ArgumentDefinition>(),
            Handler: _ => Execute(selections, () => selections.Clear(), "Challenges cleared.")));
        foreach (bool enable in new[] { true, false })
        {
            string verb = enable ? "select" : "unselect";
            grammar.Register(new(verb, "challenges", "Change selected challenges",
                new[] { new ArgumentDefinition("names", "Comma-separated challenge IDs or all",
                    Completion: ArgumentCompletion.Catalog, CommaSeparated: true,
                    CatalogProvider: () => registry.List().Select(x => x.Id).Append("all").ToArray()) },
                Handler: invocation => Execute(selections, () =>
                {
                    bool all = invocation.AllSelections?.Contains("names", StringComparer.OrdinalIgnoreCase) ?? false;
                    IReadOnlyList<string> ids;
                    if (all) ids = registry.List().Select(x => x.Id).ToArray();
                    else if (invocation.SelectionMembers is not null && invocation.SelectionMembers.TryGetValue("names", out var members)) ids = members;
                    else if (CommandTextParser.TrySplitCommaList(invocation.Arguments[0], out var parsed)) ids = parsed;
                    else throw new ArgumentException("Invalid challenge IDs.");
                    selections.Change(ids, enable);
                }, $"Challenges {(enable ? "selected" : "unselected")}.")));
        }
    }

    private static CommandOutcome ListChallenges(ChallengeRegistry registry, string[] ids) =>
        CommandOutcome.Ok(string.Join(", ", ids.Select(id =>
        {
            if (!registry.TryGet(id, out var rule) || rule == null) return id;
            var d = rule.Descriptor;
            return string.IsNullOrWhiteSpace(d.Author) ? d.Name : $"{d.Name} ({d.Author})";
        })), new CommandResultDetails(
            Data: new Dictionary<string, object?> { ["ids"] = ids, ["count"] = ids.Length }));

    private static CommandOutcome Execute(ChallengeSelections selections, Action action, string message)
    {
        string[] before = selections.Selected.ToArray();
        try
        {
            action();
            string[] after = selections.Selected.ToArray();
            bool changed = !before.SequenceEqual(after, StringComparer.OrdinalIgnoreCase);
            return CommandOutcome.Ok(message, new CommandResultDetails(
                Data: new Dictionary<string, object?> { ["selectedIds"] = after },
                Changes: changed ? new[] { new CommandStateChange("challenges", "selected", before, after) } :
                    Array.Empty<CommandStateChange>()));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException)
        {
            CommandErrorKind error = ex switch
            {
                ArgumentException => CommandErrorKind.Validation,
                InvalidOperationException => CommandErrorKind.Conflict,
                _ => CommandErrorKind.Persistence
            };
            return CommandOutcome.Failure(ex.Message, error);
        }
    }
}
