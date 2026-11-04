namespace TowerDominion.Toolkit.Modules;

/// <summary>Headless parse diagnostics with registry-derived contextual syntax.</summary>
public sealed class CommandDiagnostics
{
    private readonly CommandGrammar grammar;
    private readonly CommandTextParser parser;
    public CommandDiagnostics(CommandGrammar grammar)
    {
        this.grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        parser = new CommandTextParser(grammar);
    }

    public CommandDiagnosticResult Analyze(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var parsed = parser.Parse(input);
        if (parsed.Invocation is not null)
            return new(true, null, Array.Empty<string>(), null);
        var help = new CommandHelp(grammar);
        string[] words = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return new(false, parsed.ErrorMessage, help.Query().Syntax, "help");
        string verb = words[0];
        if (!grammar.Verbs.Contains(verb, StringComparer.OrdinalIgnoreCase))
            return new(false, parsed.ErrorMessage, help.Query().Syntax, "help");
        var operations = grammar.ForVerb(verb);
        var matches = operations.Where(operation =>
        {
            var path = new List<string>();
            if (operation.Qualifier is not null) path.Add(operation.Qualifier);
            path.AddRange(operation.Domain.Split(' '));
            int length = Math.Min(words.Length - 1, path.Count);
            return Enumerable.Range(0, length).All(i =>
                words[i + 1].Equals(path[i], StringComparison.OrdinalIgnoreCase));
        }).ToArray();
        if (matches.Length == 0)
            return new(false, parsed.ErrorMessage, operations.Select(o => o.Syntax).ToArray(), $"help {verb}");
        string? domain = matches.Select(x => x.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1
            ? matches[0].Domain : null;
        return new(false, parsed.ErrorMessage, matches.Select(o => o.Syntax).ToArray(),
            domain is null ? $"help {verb}" : $"help {verb} {domain}");
    }
}

public sealed record CommandDiagnosticResult(
    bool Success, string? Error, IReadOnlyList<string> Syntax, string? HelpCommand);
