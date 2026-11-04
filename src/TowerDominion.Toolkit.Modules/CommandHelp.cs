namespace TowerDominion.Toolkit.Modules;

/// <summary>Headless, registry-derived help queries. Formatting remains a presentation concern.</summary>
public sealed class CommandHelp
{
    private readonly CommandGrammar grammar;
    public CommandHelp(CommandGrammar grammar) => this.grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));

    public CommandHelpResult Query(string? target = null)
    {
        string[] words = (target ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 0 && words[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            words = words.Skip(1).ToArray();
        var operations = grammar.Operations;
        if (words.Length == 0)
            return new(true, "Commands", operations.ToArray(), grammar.Verbs.ToArray(),
                grammar.Domains.Select(d => d.Name).ToArray());
        string input = string.Join(" ", words);
        if (grammar.Verbs.Contains(input, StringComparer.OrdinalIgnoreCase))
            return Result(input, grammar.ForVerb(input));
        if (grammar.Domains.Any(d => d.Name.Equals(input, StringComparison.OrdinalIgnoreCase)))
            return Result(input, grammar.ForDomain(input));
        if (words.Length > 1 && grammar.Verbs.Contains(words[0], StringComparer.OrdinalIgnoreCase))
        {
            string domain = string.Join(" ", words.Skip(1));
            var matches = grammar.ForVerb(words[0]).Where(o =>
                o.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase)).ToArray();
            return matches.Length > 0 ? Result(input, matches) :
                new(false, $"Unsupported command: {input}.", Array.Empty<OperationDefinition>());
        }
        return new(false, $"Unknown help target: {input}.", Array.Empty<OperationDefinition>());
    }

    private static CommandHelpResult Result(string title, IReadOnlyList<OperationDefinition> operations) =>
        new(true, title, operations,
            operations.Select(o => o.Verb).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToArray(),
            operations.Select(o => o.Domain).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d).ToArray());
}

public sealed record CommandHelpResult(
    bool Success,
    string Title,
    IReadOnlyList<OperationDefinition> Operations,
    IReadOnlyList<string>? Verbs = null,
    IReadOnlyList<string>? Domains = null)
{
    public IReadOnlyList<string> Syntax => Operations.Select(o => o.Syntax).ToArray();
}
