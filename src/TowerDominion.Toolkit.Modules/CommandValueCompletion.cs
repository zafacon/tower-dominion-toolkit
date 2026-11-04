namespace TowerDominion.Toolkit.Modules;

/// <summary>Quote- and selection-aware completion for one argument's raw text.</summary>
public static class CommandValueCompletion
{
    public static CommandCompletionResult Complete(string input, int cursor, IEnumerable<string> candidates, bool commaSeparated)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(candidates);
        if (cursor < 0 || cursor > input.Length) throw new ArgumentOutOfRangeException(nameof(cursor));
        var boundaries = new List<int> { 0 };
        bool quoted = false, escaped = false;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (escaped) { escaped = false; continue; }
            if (quoted && c == '\\') { escaped = true; continue; }
            if (c == '"') { quoted = !quoted; continue; }
            if (!quoted && (commaSeparated ? c == ',' : char.IsWhiteSpace(c)))
                boundaries.Add(i + 1);
        }
        int start = boundaries.Last(x => x <= cursor);
        int end = boundaries.Where(x => x > cursor).Select(x => x - 1).DefaultIfEmpty(input.Length).First();
        while (start < cursor && char.IsWhiteSpace(input[start])) start++;
        while (end > cursor && char.IsWhiteSpace(input[end - 1])) end--;
        bool openingQuote = start < end && input[start] == '"';
        bool closingQuote = end > start && input[end - 1] == '"' && !IsEscaped(input, end - 1);
        int valueStart = openingQuote ? start + 1 : start;
        int valueEnd = closingQuote ? end - 1 : end;
        // The caret can be before an opening quote, or between a delimiter
        // and whitespace. Do not clamp against a reversed/empty value span.
        valueEnd = Math.Max(valueStart, valueEnd);
        int valueCursor = Math.Clamp(cursor, valueStart, valueEnd);
        string prefix = Decode(input.Substring(valueStart, valueCursor - valueStart));
        string suffix = Decode(input.Substring(valueCursor, valueEnd - valueCursor));
        var matches = candidates.Where(candidate => candidate is not null &&
                candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                candidate.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                candidate.Length >= prefix.Length + suffix.Length)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        if (matches.Length == 0) return new(input, cursor, start, end, matches, false);
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
        string value = matches.Length == 1 ? matches[0] : expansion;
        bool hasOtherMembers = commaSeparated && input.Contains(',');
        bool needsQuotes = openingQuote || value.Any(char.IsWhiteSpace) || value.Contains('"') || value.Contains('\\') ||
            (commaSeparated && (value.Contains(',') || (hasOtherMembers && value.Equals("all", StringComparison.OrdinalIgnoreCase))));
        string replacement = needsQuotes ? "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"" : value;
        string text = string.Concat(input.AsSpan(0, start), replacement.AsSpan(), input.AsSpan(end));
        return new(text, start + replacement.Length, start, end, matches, text != input);
    }

    private static bool IsEscaped(string input, int index)
    {
        int slashes = 0;
        while (index > slashes && input[index - slashes - 1] == '\\') slashes++;
        return slashes % 2 != 0;
    }

    private static string Decode(string raw)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            if (raw[i] == '\\' && i + 1 < raw.Length) i++;
            result.Append(raw[i]);
        }
        return result.ToString();
    }
}
