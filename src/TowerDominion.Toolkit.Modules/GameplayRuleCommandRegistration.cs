namespace TowerDominion.Toolkit.Modules;

public static class GameplayRuleCommandRegistration
{
    public static void Register(CommandGrammar grammar, GameplayRules rules, string presetDirectory)
    {
        grammar.RegisterDomain(new("rules", "Optional gameplay rules"));
        var store = new CommandPresetStore<string[]>(presetDirectory, ids =>
        {
            try { GameplayRules.Validate(ids); return null; }
            catch (ArgumentException ex) { return ex.Message; }
        });
        string[] All() => GameplayRules.Catalog.Select(x => x.Id).ToArray();
        grammar.Register(new("list", "rules", "List gameplay rules", Array.Empty<ArgumentDefinition>(),
            Handler: _ => CommandOutcome.Ok(string.Join("\n", GameplayRules.Catalog.Select(x => $"{x.Id}: {(rules.IsEnabled(x.Id) ? "enabled" : "disabled")}")))));
        grammar.Register(new("list", "rules", "List enabled gameplay rules", Array.Empty<ArgumentDefinition>(), Qualifier: "enabled",
            Handler: _ => CommandOutcome.Ok(string.Join(", ", rules.Enabled))));
        grammar.Register(new("show", "rules", "Show a gameplay rule", new[] { new ArgumentDefinition("id", "Rule ID", Completion: ArgumentCompletion.Catalog, CatalogProvider: All) },
            Handler: i => GameplayRules.Catalog.FirstOrDefault(x => x.Id.Equals(i.Arguments[0], StringComparison.OrdinalIgnoreCase)) is { } r
                ? CommandOutcome.Ok($"{r.Name}\nID: {r.Id}\n{r.Description}\nRequested: {(rules.IsEnabled(r.Id) ? "enabled" : "disabled")}")
                : CommandOutcome.Failure("Unknown rule.", CommandErrorKind.Validation)));
        foreach (bool on in new[] { true, false })
        {
            grammar.Register(new(on ? "enable" : "disable", "rules", "Change requested gameplay rules",
                new[] { new ArgumentDefinition("ids", "Comma-separated IDs or all", Completion: ArgumentCompletion.Catalog, CommaSeparated: true,
                    CatalogProvider: () => All().Append("all").ToArray()) },
                Handler: i => Execute(() =>
                {
                    var ids = i.AllSelections?.Contains("ids", StringComparer.OrdinalIgnoreCase) == true ? All() :
                        i.SelectionMembers is not null && i.SelectionMembers.TryGetValue("ids", out var members) ? members.ToArray() :
                        CommandTextParser.TrySplitCommaList(i.Arguments[0], out var parsed) ? parsed.ToArray() : throw new ArgumentException("Invalid IDs.");
                    rules.Change(ids, on);
                    return $"Rules {(on ? "enabled" : "disabled")}: {string.Join(", ", ids)}.";
                })));
        }
        grammar.Register(new("clear", "rules", "Disable all gameplay rules", Array.Empty<ArgumentDefinition>(),
            Handler: _ => Execute(() => { rules.Replace(Array.Empty<string>()); return "All rules disabled."; })));
        grammar.RegisterPresets("rules", new PresetHandlers(
            i => Execute(() => { store.Save(i.Arguments[0], rules.Enabled.ToArray()); return "Rules preset saved."; }),
            i => Execute(() => { rules.Replace(store.Read(i.Arguments[0])); return "Rules preset loaded."; }),
            _ => CommandOutcome.Ok(string.Join(", ", store.Names())),
            i => Execute(() => string.Join(", ", store.Read(i.Arguments[0]))),
            i => Execute(() => store.Delete(i.Arguments[0]) ? "Rules preset deleted." : "Rules preset not found.")), store.Names);
    }
    private static CommandOutcome Execute(Func<string> action)
    {
        try { return CommandOutcome.Ok(action()); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or System.Text.Json.JsonException)
        { return CommandOutcome.Failure(ex.Message, ex is ArgumentException ? CommandErrorKind.Validation : CommandErrorKind.Persistence); }
    }
}
