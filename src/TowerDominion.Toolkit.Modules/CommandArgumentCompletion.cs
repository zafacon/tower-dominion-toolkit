namespace TowerDominion.Toolkit.Modules;

/// <summary>Resolves registered argument values and applies quote-aware completion.</summary>
public sealed class CommandArgumentCompletion
{
    private readonly CommandGrammar grammar;
    public CommandArgumentCompletion(CommandGrammar grammar) => this.grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));

    public CommandArgumentCompletionResult Complete(string input, int cursor)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (cursor < 0 || cursor > input.Length) throw new ArgumentOutOfRangeException(nameof(cursor));
        foreach (var operation in grammar.Operations)
        {
            var path = new List<string> { operation.Verb };
            if (operation.Qualifier is not null) path.Add(operation.Qualifier);
            path.AddRange(operation.Domain.Split(' '));
            if (operation.IsPresetOperation)
            {
                string? marker = operation.Verb.ToLowerInvariant() switch
                {
                    "save" => "as",
                    "list" => "presets",
                    "inspect" or "delete" => "preset",
                    _ => null
                };
                if (marker is not null) path.Add(marker);
            }
            int position = 0;
            bool matches = true;
            foreach (string word in path)
            {
                while (position < input.Length && char.IsWhiteSpace(input[position])) position++;
                int start = position;
                while (position < input.Length && !char.IsWhiteSpace(input[position])) position++;
                if (start == position || !input.Substring(start, position - start).Equals(word, StringComparison.OrdinalIgnoreCase))
                { matches = false; break; }
            }
            if (!matches || position >= cursor || position >= input.Length || !char.IsWhiteSpace(input[position])) continue;
            while (position < input.Length && char.IsWhiteSpace(input[position])) position++;
            if (position > cursor || operation.Arguments.Count == 0) continue;
            // For multiple positional arguments, locate the current quote-aware token
            // and restrict replacement to its span (not the entire argument suffix).
            int argumentIndex = 0;
            int segmentStart = position;
            int segmentEnd = input.Length;
            if (operation.Arguments.Count > 1)
            {
                var spans = new List<(int Start, int End)>();
                bool inQuote = false, escaped = false;
                int tokenStart = -1;
                for (int i = position; i < input.Length; i++)
                {
                    char c = input[i];
                    if (!inQuote && char.IsWhiteSpace(c))
                    {
                        if (tokenStart >= 0) { spans.Add((tokenStart, i)); tokenStart = -1; }
                        continue;
                    }
                    if (tokenStart < 0) tokenStart = i;
                    if (escaped) { escaped = false; continue; }
                    if (inQuote && c == '\\') { escaped = true; continue; }
                    if (c == '"') inQuote = !inQuote;
                }
                if (tokenStart >= 0) spans.Add((tokenStart, input.Length));
                argumentIndex = spans.FindIndex(span => cursor >= span.Start && cursor <= span.End);
                if (argumentIndex < 0)
                {
                    argumentIndex = spans.Count(span => span.End <= cursor);
                    segmentStart = cursor;
                    segmentEnd = cursor;
                }
                else
                {
                    segmentStart = spans[argumentIndex].Start;
                    segmentEnd = spans[argumentIndex].End;
                }
            }
            if (argumentIndex >= operation.Arguments.Count) continue;
            var argument = operation.Arguments[argumentIndex];
            IReadOnlyList<string>? values = argument.AllowedValues;
            if (values is null && argument.Completion is ArgumentCompletion.Catalog or ArgumentCompletion.Preset)
                values = (argument.CompletionProvider ?? argument.CatalogProvider)?.Invoke();
            if (values is null)
                return new(CommandCompletion.Complete(input, cursor, Array.Empty<string>()),
                    argument.Completion is ArgumentCompletion.Catalog or ArgumentCompletion.Preset
                        ? ArgumentCompletionStatus.Unavailable : ArgumentCompletionStatus.Unrestricted,
                    operation, argument);
            string segment = input.Substring(segmentStart, segmentEnd - segmentStart);
            var completed = CommandValueCompletion.Complete(segment, cursor - segmentStart, values, argument.CommaSeparated);
            string fullText = string.Concat(input.AsSpan(0, segmentStart), completed.Text.AsSpan(), input.AsSpan(segmentEnd));
            return new(new(fullText,
                segmentStart + completed.Cursor, segmentStart + completed.ReplaceStart,
                segmentStart + completed.ReplaceEnd, completed.Candidates, completed.Changed),
                ArgumentCompletionStatus.Available, operation, argument);
        }
        return new(CommandCompletion.Complete(input, cursor, Array.Empty<string>()),
            ArgumentCompletionStatus.NotArgument, null, null);
    }
}

public enum ArgumentCompletionStatus { NotArgument, Available, Unrestricted, Unavailable }

public sealed record CommandArgumentCompletionResult(
    CommandCompletionResult Completion, ArgumentCompletionStatus Status,
    OperationDefinition? Operation, ArgumentDefinition? Argument);
