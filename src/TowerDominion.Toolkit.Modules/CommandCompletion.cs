namespace TowerDominion.Toolkit.Modules;

/// <summary>Headless cursor-aware completion mechanics; candidate discovery is supplied by later slices.</summary>
public static class CommandCompletion
{
    public static CommandCompletionResult Complete(string input, int cursor, IEnumerable<string> candidates)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(candidates);
        if (cursor < 0 || cursor > input.Length) throw new ArgumentOutOfRangeException(nameof(cursor));

        int start = cursor;
        while (start > 0 && !char.IsWhiteSpace(input[start - 1])) start--;
        int end = cursor;
        while (end < input.Length && !char.IsWhiteSpace(input[end])) end++;
        string prefix = input.Substring(start, cursor - start);
        string suffix = input.Substring(cursor, end - cursor);
        var matches = candidates.Where(candidate => candidate is not null &&
                candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                candidate.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                candidate.Length >= prefix.Length + suffix.Length)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(candidate => candidate, StringComparer.OrdinalIgnoreCase).ToArray();
        if (matches.Length == 0)
            return new(input, cursor, start, end, matches, false);

        string expansion = matches[0];
        for (int i = 1; i < matches.Length; i++)
        {
            int common = 0;
            while (common < expansion.Length && common < matches[i].Length &&
                char.ToUpperInvariant(expansion[common]) == char.ToUpperInvariant(matches[i][common])) common++;
            expansion = expansion.Substring(0, common);
        }
        if (matches.Length > 1 && expansion.Length <= prefix.Length)
            return new(input, cursor, start, end, matches, false);
        string replacement = matches.Length == 1 ? matches[0] : expansion;
        string updated = string.Concat(input.AsSpan(0, start), replacement.AsSpan(), input.AsSpan(end));
        return new(updated, start + replacement.Length, start, end, matches, updated != input);
    }
}

public sealed record CommandCompletionResult(
    string Text, int Cursor, int ReplaceStart, int ReplaceEnd,
    IReadOnlyList<string> Candidates, bool Changed);
