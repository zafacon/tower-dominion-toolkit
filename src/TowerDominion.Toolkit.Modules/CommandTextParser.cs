namespace TowerDominion.Toolkit.Modules;

/// <summary>Registry-driven raw command parser, independent of game state.</summary>
public sealed class CommandTextParser
{
    private readonly CommandGrammar grammar;
    public CommandTextParser(CommandGrammar grammar) => this.grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
    public CommandParseResult Parse(string input)
    {
        if (!TryTokenize(input, out var tokens, out var error))
            return CommandParseResult.Error(error!);
        if (tokens.Count == 0) return CommandParseResult.Error("Empty command.");
        if (tokens[0].Quoted) return CommandParseResult.Error("Command verb cannot be quoted.");
        string verb = tokens[0].Value;
        if (!grammar.Verbs.Contains(verb, StringComparer.OrdinalIgnoreCase))
            return CommandParseResult.Error($"Unknown verb: {verb}. Available verbs: {string.Join(", ", grammar.Verbs)}.");

        var candidates = new List<(OperationDefinition Operation, int Consumed)>();
        var incompleteMarkers = new List<string>();
        var matchedDomain = false;
        foreach (var operation in grammar.ForVerb(verb))
        {
            int cursor = 1;
            if (operation.Qualifier is not null)
            {
                if (cursor >= tokens.Count || !EqualsToken(tokens[cursor++], operation.Qualifier)) continue;
            }
            string[] domainWords = operation.Domain.Split(' ');
            if (cursor + domainWords.Length > tokens.Count) continue;
            if (!domainWords.Select((word, i) => EqualsToken(tokens[cursor + i], word)).All(x => x)) continue;
            cursor += domainWords.Length;
            matchedDomain = true;
            if (operation.IsPresetOperation)
            {
                // Save uses "as"; list uses "presets"; inspect/delete use "preset".
                // Load has no literal marker.
                string? marker = operation.Verb.ToLowerInvariant() switch
                {
                    "save" => "as",
                    "list" => "presets",
                    "inspect" or "delete" => "preset",
                    _ => null
                };
                if (marker is not null)
                {
                    if (cursor >= tokens.Count || !EqualsToken(tokens[cursor], marker))
                    {
                        incompleteMarkers.Add($"Expected '{marker}' after {operation.Verb} {operation.Domain}.");
                        continue;
                    }
                    cursor++;
                }
            }
            candidates.Add((operation, cursor));
        }
        if (candidates.Count == 0)
        {
            if (matchedDomain && incompleteMarkers.Count > 0)
                return CommandParseResult.Error(incompleteMarkers[0]);
            string supported = string.Join(", ", grammar.ForVerb(verb)
                .Select(o => o.Domain).Distinct(StringComparer.OrdinalIgnoreCase));
            return CommandParseResult.Error($"Unsupported domain or qualifier for {verb}. Supported domains: {supported}.");
        }

        var valid = new List<CommandInvocation>();
        var errors = new List<string>();
        foreach (var (operation, consumed) in candidates)
        {
            var remaining = tokens.Skip(consumed).ToArray();
            var arguments = new List<string>();
            if (operation.Arguments.Count == 1 && remaining.Length > 0 &&
                (operation.Arguments[0].Completion is ArgumentCompletion.FreeForm or ArgumentCompletion.Preset ||
                 operation.Arguments[0].CommaSeparated))
            {
                // A single free-form argument may be a multiword name; selection lists
                // are normalized separately by domain validators in a later phase.
                arguments.Add(string.Join(" ", remaining.Select(t => operation.Arguments[0].CommaSeparated ? t.Raw : t.Value)));
            }
            else arguments.AddRange(remaining.Select(t => t.Value));
            if (arguments.Count < operation.Arguments.Count(a => a.Required) ||
                arguments.Count > operation.Arguments.Count)
            {
                errors.Add($"Invalid argument count for {operation.Verb} {operation.Domain}: expected {operation.Arguments.Count(a => a.Required)}–{operation.Arguments.Count}.");
                continue;
            }
            bool semanticValid = true;
            var selections = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var allSelections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < arguments.Count; i++)
            {
                ArgumentDefinition definition = operation.Arguments[i];
                if (string.IsNullOrWhiteSpace(arguments[i])) { errors.Add($"Argument {definition.Name} cannot be empty."); semanticValid = false; break; }
                if (definition.AllowedValues is not null &&
                    !definition.AllowedValues.Contains(arguments[i], StringComparer.OrdinalIgnoreCase))
                { errors.Add($"Invalid {definition.Name}: expected {string.Join(", ", definition.AllowedValues)}."); semanticValid = false; break; }
                if (definition.CommaSeparated)
                {
                    if (!TrySplitCommaList(arguments[i], out var members))
                    { errors.Add($"Invalid comma-separated {definition.Name}."); semanticValid = false; break; }
                    bool reservedAll = remaining.Length == 1 && !remaining[0].Quoted &&
                        remaining[0].Value.Equals("all", StringComparison.OrdinalIgnoreCase);
                    if (reservedAll) allSelections.Add(definition.Name);
                    else
                    {
                        if (members.Count > 1 && HasUnquotedAllMember(arguments[i]))
                        { errors.Add("Reserved all must be used alone or quoted as a literal member."); semanticValid = false; break; }
                        selections.Add(definition.Name, members);
                    }
                }
                if (definition.Validator is not null)
                {
                    string? validationError = definition.Validator(arguments[i]);
                    if (validationError is not null) { errors.Add(validationError); semanticValid = false; break; }
                }
            }
            if (!semanticValid) continue;
            valid.Add(new(operation.Verb, operation.Domain, arguments, operation.Qualifier, operation.Variant, selections, allSelections));
        }
        if (valid.Count == 0) return CommandParseResult.Error(
            errors.Count > 0 ? errors[0] : "Invalid or missing arguments.", CommandErrorKind.Validation);
        if (valid.Count != 1) return CommandParseResult.Error("Ambiguous command.");
        return new(valid[0], null);
    }

    public CommandOutcome Execute(string input)
    {
        var result = Parse(input);
        return result.Invocation is null
            ? CommandOutcome.Failure(result.ErrorMessage!, result.ErrorKind)
            : grammar.Dispatch(result.Invocation);
    }

    /// <summary>Split comma-separated selections while respecting quoted members.</summary>
    public static bool TrySplitCommaList(string input, out IReadOnlyList<string> members)
    {
        var result = new List<string>();
        var part = new System.Text.StringBuilder();
        bool inQuotes = false, escaping = false;
        foreach (char c in input)
        {
            if (escaping) { part.Append(c); escaping = false; continue; }
            if (inQuotes && c == '\\') { escaping = true; continue; }
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes)
            {
                string item = part.ToString().Trim();
                if (item.Length == 0) { members = Array.Empty<string>(); return false; }
                result.Add(item);
                part.Clear();
                continue;
            }
            part.Append(c);
        }
        string last = part.ToString().Trim();
        if (inQuotes || escaping || last.Length == 0)
        { members = Array.Empty<string>(); return false; }
        result.Add(last);
        members = result;
        return true;
    }

    internal static bool HasUnquotedAllMember(string raw)
    {
        bool quoted = false, escaping = false;
        var part = new System.Text.StringBuilder();
        foreach (char c in raw)
        {
            if (escaping) { part.Append(c); escaping = false; continue; }
            if (quoted && c == '\\') { escaping = true; part.Append(c); continue; }
            if (c == '"') { quoted = !quoted; part.Append(c); continue; }
            if (c == ',' && !quoted)
            {
                if (part.ToString().Trim().Equals("all", StringComparison.OrdinalIgnoreCase)) return true;
                part.Clear();
                continue;
            }
            part.Append(c);
        }
        return part.ToString().Trim().Equals("all", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EqualsToken(CommandToken token, string literal) =>
        !token.Quoted && token.Value.Equals(literal, StringComparison.OrdinalIgnoreCase);

    public static bool TryTokenize(string input, out IReadOnlyList<CommandToken> tokens, out string? error)
    {
        var output = new List<CommandToken>();
        var value = new System.Text.StringBuilder();
        var raw = new System.Text.StringBuilder();
        bool quoted = false, inQuotes = false, started = false, escaping = false;
        foreach (char c in input ?? "")
        {
            if (escaping) { value.Append(c); raw.Append(c); escaping = false; started = true; continue; }
            if (c == '\\' && inQuotes) { raw.Append(c); escaping = true; continue; }
            if (c == '"') { raw.Append(c); inQuotes = !inQuotes; quoted = true; started = true; continue; }
            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (started) { output.Add(new(value.ToString(), quoted, raw.ToString())); value.Clear(); raw.Clear(); started = quoted = false; }
                continue;
            }
            value.Append(c); raw.Append(c); started = true;
        }
        if (inQuotes || escaping)
        {
            tokens = Array.Empty<CommandToken>();
            error = "Unterminated quoted argument.";
            return false;
        }
        if (started) output.Add(new(value.ToString(), quoted, raw.ToString()));
        tokens = output;
        error = null;
        return true;
    }
}

public sealed record CommandToken(string Value, bool Quoted, string Raw = "");
public sealed record CommandParseResult(CommandInvocation? Invocation, string? ErrorMessage)
{
    public CommandErrorKind ErrorKind { get; init; } = CommandErrorKind.None;

    public static CommandParseResult Error(string message, CommandErrorKind kind = CommandErrorKind.Syntax) =>
        new(null, message) { ErrorKind = kind };
}
