namespace TowerDominion.Toolkit.Modules;

/// <summary>Discovers fixed grammar tokens; dynamic argument candidates are a separate concern.</summary>
public sealed class CommandStructuralCompletion
{
    private static readonly string[] HelpPrefix = { "help" };
    private readonly CommandGrammar grammar;
    public CommandStructuralCompletion(CommandGrammar grammar) => this.grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));

    public CommandCompletionResult Complete(string input, int cursor)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (cursor < 0 || cursor > input.Length) throw new ArgumentOutOfRangeException(nameof(cursor));
        int start = cursor;
        while (start > 0 && !char.IsWhiteSpace(input[start - 1])) start--;
        string before = input.Substring(0, start);
        var completed = before.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var paths = new List<string[]>();
        foreach (var operation in grammar.Operations)
        {
            var parts = new List<string> { operation.Verb };
            if (operation.Qualifier is not null) parts.Add(operation.Qualifier);
            parts.AddRange(operation.Domain.Split(' '));
            if (operation.IsPresetOperation)
            {
                string? marker = operation.Verb.ToLowerInvariant() switch
                {
                    "save" => "as",
                    "list" => "presets",
                    "inspect" or "delete" => "preset",
                    _ => null
                };
                if (marker is not null) parts.Add(marker);
            }
            paths.Add(parts.ToArray());
        }
        if (completed.Length > 0 && completed[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            paths.Clear();
            paths.AddRange(grammar.Verbs.Select(verb => new[] { "help", verb }));
            paths.AddRange(grammar.Domains.Select(domain => HelpPrefix.Concat(domain.Name.Split(' ')).ToArray()));
            paths.AddRange(grammar.Operations.Select(operation =>
                new[] { "help", operation.Verb }.Concat(operation.Domain.Split(' ')).ToArray()));
        }
        else paths.Add(HelpPrefix);
        var candidates = paths.Where(path => path.Length > completed.Length &&
                completed.Select((word, index) => word.Equals(path[index], StringComparison.OrdinalIgnoreCase)).All(match => match))
            .Select(path => path[completed.Length]);
        return CommandCompletion.Complete(input, cursor, candidates);
    }
}
