namespace TowerDominion.Toolkit.Modules;

/// <summary>Game-neutral declarative grammar for Toolkit commands.</summary>
public sealed class CommandGrammar
{
    private readonly Dictionary<string, DomainDefinition> domains = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OperationDefinition> operations = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> verbs = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<DomainDefinition> Domains => domains.Values.OrderBy(x => x.Name).ToArray();
    public IReadOnlyCollection<OperationDefinition> Operations => operations.Values.OrderBy(x => x.Domain).ThenBy(x => x.Verb).ToArray();
    public IReadOnlyCollection<string> Verbs => verbs.OrderBy(x => x).ToArray();

    public void RegisterDomain(DomainDefinition domain)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ValidateName(domain.Name, nameof(domain));
        if (!domains.TryAdd(domain.Name, domain))
            throw new InvalidOperationException($"Duplicate domain: {domain.Name}");
        if (verbs.Contains(domain.Name))
        {
            domains.Remove(domain.Name);
            throw new InvalidOperationException($"Domain conflicts with verb: {domain.Name}");
        }
    }

    public void Register(OperationDefinition operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ValidateName(operation.Verb, nameof(operation));
        if (operation.Verb.Contains(' '))
            throw new ArgumentException("Verbs must be single tokens.");
        if (!domains.ContainsKey(operation.Domain))
            throw new InvalidOperationException($"Unregistered domain: {operation.Domain}");
        if (domains.ContainsKey(operation.Verb))
            throw new InvalidOperationException($"Verb conflicts with domain: {operation.Verb}");
        if (operation.Qualifiers.Any(q => string.IsNullOrWhiteSpace(q) || q.Any(char.IsWhiteSpace)))
            throw new ArgumentException("Qualifiers must be single tokens.");
        if (operation.Arguments.Any(a => string.IsNullOrWhiteSpace(a.Name)))
            throw new ArgumentException("Argument names cannot be empty.");
        if (operation.Arguments.Select(a => a.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != operation.Arguments.Count)
            throw new ArgumentException("Argument names must be unique, ignoring case.");
        if (operation.Arguments.Select((a, i) => (a, i)).Any(x => !x.a.Required &&
            operation.Arguments.Skip(x.i + 1).Any(a => a.Required)))
            throw new ArgumentException("Required arguments cannot follow optional arguments.");
        if (operation.Arguments.Any(a => a.AllowedValues is { Count: 0 } ||
            (a.AllowedValues is not null && a.AllowedValues.Any(string.IsNullOrWhiteSpace))))
            throw new ArgumentException("Allowed values must be nonempty.");
        if (operation.Arguments.Any(a => a.AllowedValues is not null &&
            a.AllowedValues.Distinct(StringComparer.OrdinalIgnoreCase).Count() != a.AllowedValues.Count))
            throw new ArgumentException("Allowed values must be unique, ignoring case.");
        string key = Key(operation.Verb, operation.Domain, operation.Qualifier, operation.Variant);
        if (operations.Values.Any(existing => PatternsOverlap(existing, operation)))
            throw new InvalidOperationException($"Overlapping command pattern: {key}");
        if (!operations.TryAdd(key, operation))
            throw new InvalidOperationException($"Duplicate operation: {key}");
        verbs.Add(operation.Verb);
    }

    private static bool PatternsOverlap(OperationDefinition a, OperationDefinition b)
    {
        if (!a.Verb.Equals(b.Verb, StringComparison.OrdinalIgnoreCase)) return false;
        static string[] Prefix(OperationDefinition o)
        {
            var words = new List<string>();
            if (o.Qualifier != null) words.Add(o.Qualifier);
            words.AddRange(o.Domain.Split(' '));
            if (o.IsPresetOperation)
            {
                string? marker = o.Verb.ToLowerInvariant() switch
                {
                    "save" => "as",
                    "list" => "presets",
                    "inspect" or "delete" => "preset",
                    _ => null
                };
                if (marker != null) words.Add(marker);
            }
            return words.ToArray();
        }
        var left = Prefix(a);
        var right = Prefix(b);
        int shared = Math.Min(left.Length, right.Length);
        if (!left.Take(shared).SequenceEqual(right.Take(shared), StringComparer.OrdinalIgnoreCase))
            return false;

        if (left.Length == right.Length)
        {
            // A single free-form/preset/list argument consumes all remaining tokens.
            // It can therefore overlap a fixed multi-argument operation.
            bool aGreedy = a.Arguments.Count == 1 &&
                (a.Arguments[0].Completion is ArgumentCompletion.FreeForm or ArgumentCompletion.Preset || a.Arguments[0].CommaSeparated);
            bool bGreedy = b.Arguments.Count == 1 &&
                (b.Arguments[0].Completion is ArgumentCompletion.FreeForm or ArgumentCompletion.Preset || b.Arguments[0].CommaSeparated);
            if (aGreedy && b.Arguments.Count > 1 && GreedyOverlapsArguments(a.Arguments[0], b.Arguments)) return true;
            if (bGreedy && a.Arguments.Count > 1 && GreedyOverlapsArguments(b.Arguments[0], a.Arguments)) return true;
            int min = Math.Max(a.Arguments.Count(x => x.Required), b.Arguments.Count(x => x.Required));
            int max = Math.Min(a.Arguments.Count, b.Arguments.Count);
            if (min > max) return false;
            for (int count = min; count <= max; count++)
            {
                if (Enumerable.Range(0, count).All(i => ValuesOverlap(a.Arguments[i], b.Arguments[i])))
                    return true;
            }
            return false;
        }

        var shorter = left.Length < right.Length ? a : b;
        var longer = left.Length < right.Length ? b : a;
        var suffix = (left.Length < right.Length ? right : left).Skip(shared).ToArray();
        // A longer registered domain can also be consumed as several ordinary
        // arguments of the shorter pattern, not only as one greedy argument.
        if (shorter.Arguments.Count >= suffix.Length && suffix.Length > 0)
        {
            bool suffixCompatible = Enumerable.Range(0, suffix.Length).All(i =>
                shorter.Arguments[i].AllowedValues is null ||
                shorter.Arguments[i].AllowedValues!.Contains(suffix[i], StringComparer.OrdinalIgnoreCase));
            int shorterRequiredAfterSuffix = Math.Max(0, shorter.Arguments.Count(x => x.Required) - suffix.Length);
            int longerRequired = longer.Arguments.Count(x => x.Required);
            if (suffixCompatible && longer.Arguments.Count == 1 &&
                (longer.Arguments[0].Completion is ArgumentCompletion.FreeForm or ArgumentCompletion.Preset ||
                 longer.Arguments[0].CommaSeparated) &&
                GreedyOverlapsArguments(longer.Arguments[0], shorter.Arguments.Skip(suffix.Length).ToArray()))
                return true;
            if (suffixCompatible &&
                Math.Max(shorterRequiredAfterSuffix, longerRequired) <=
                Math.Min(shorter.Arguments.Count - suffix.Length, longer.Arguments.Count))
            {
                int sharedArguments = Math.Max(shorterRequiredAfterSuffix, longerRequired);
                if (Enumerable.Range(0, sharedArguments).All(i =>
                    ValuesOverlap(shorter.Arguments[suffix.Length + i], longer.Arguments[i])))
                    return true;
            }
        }
        if (shorter.Arguments.Count != 1) return false;
        var arg = shorter.Arguments[0];
        if (arg.Completion is not (ArgumentCompletion.FreeForm or ArgumentCompletion.Preset) &&
            !arg.CommaSeparated) return false;
        if (arg.AllowedValues is not null)
        {
            // A finite greedy value must contain the entire longer prefix and
            // a valid number of trailing arguments, not merely start with it.
            return arg.AllowedValues.Any(value =>
            {
                string[] words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < suffix.Length ||
                    !words.Take(suffix.Length).SequenceEqual(suffix, StringComparer.OrdinalIgnoreCase))
                    return false;
                int remaining = words.Length - suffix.Length;
                return remaining >= longer.Arguments.Count(x => x.Required) &&
                    remaining <= longer.Arguments.Count &&
                    Enumerable.Range(0, remaining).All(i =>
                        longer.Arguments[i].AllowedValues is null ||
                        longer.Arguments[i].AllowedValues!.Contains(words[suffix.Length + i], StringComparer.OrdinalIgnoreCase));
            });
        }
        // An unconstrained greedy value can consume the longer prefix and arguments.
        return true;
    }

    private static bool GreedyOverlapsArguments(ArgumentDefinition greedy, IReadOnlyList<ArgumentDefinition> other)
    {
        if (greedy.AllowedValues is null) return true;
        int required = other.Count(x => x.Required);
        return greedy.AllowedValues.Any(value =>
        {
            string[] words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length >= required && words.Length <= other.Count &&
                Enumerable.Range(0, words.Length).All(i =>
                    other[i].AllowedValues is null ||
                    other[i].AllowedValues!.Contains(words[i], StringComparer.OrdinalIgnoreCase));
        });
    }

    private static bool ValuesOverlap(ArgumentDefinition a, ArgumentDefinition b)
    {
        if (a.AllowedValues == null || b.AllowedValues == null) return true;
        return a.AllowedValues.Intersect(b.AllowedValues, StringComparer.OrdinalIgnoreCase).Any();
    }

    public bool Supports(string verb, string domain, string? qualifier = null, string? variant = null) =>
        operations.ContainsKey(Key(verb, domain, qualifier, variant));

    public OperationDefinition? Find(string verb, string domain, string? qualifier = null, string? variant = null) =>
        operations.GetValueOrDefault(Key(verb, domain, qualifier, variant));

    /// <summary>Dispatches an already-parsed invocation; parsing is a separate phase.</summary>
    public CommandOutcome Dispatch(CommandInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        OperationDefinition? operation = Find(invocation.Verb, invocation.Domain, invocation.Qualifier, invocation.Variant);
        if (operation == null) return CommandOutcome.Failure("Unsupported operation.", CommandErrorKind.Syntax);
        if (operation.Handler == null) return CommandOutcome.Failure("Operation handler is unavailable.", CommandErrorKind.Unavailable);
        if (invocation.Arguments.Count < operation.Arguments.Count(a => a.Required) ||
            invocation.Arguments.Count > operation.Arguments.Count)
            return CommandOutcome.Failure("Invalid argument count.", CommandErrorKind.Validation);
        var selectionNames = operation.Arguments.Take(invocation.Arguments.Count)
            .Where(a => a.CommaSeparated).Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if ((invocation.AllSelections?.Any(name => !selectionNames.Contains(name)) ?? false) ||
            (invocation.SelectionMembers?.Keys.Any(name => !selectionNames.Contains(name)) ?? false))
            return CommandOutcome.Failure("Selection metadata references an unsupported argument.", CommandErrorKind.Validation);
        if ((invocation.AllSelections is not null && invocation.AllSelections.Distinct(StringComparer.OrdinalIgnoreCase).Count() != invocation.AllSelections.Count) ||
            (invocation.SelectionMembers is not null && invocation.SelectionMembers.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != invocation.SelectionMembers.Count))
            return CommandOutcome.Failure("Duplicate selection metadata argument.", CommandErrorKind.Validation);
        if (invocation.SelectionMembers?.Any(entry => entry.Value is null || entry.Value.Any(member => member is null)) ?? false)
            return CommandOutcome.Failure("Selection metadata contains null members.", CommandErrorKind.Validation);
        for (int i = 0; i < invocation.Arguments.Count; i++)
        {
            var definition = operation.Arguments[i];
            var value = invocation.Arguments[i];
            if (string.IsNullOrWhiteSpace(value))
                return CommandOutcome.Failure($"Argument {definition.Name} cannot be empty.", CommandErrorKind.Validation);
            if (definition.AllowedValues is not null &&
                !definition.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                return CommandOutcome.Failure($"Invalid {definition.Name}.", CommandErrorKind.Validation);
            if (definition.CommaSeparated)
            {
                bool all = invocation.AllSelections?.Contains(definition.Name, StringComparer.OrdinalIgnoreCase) ?? false;
                IReadOnlyList<string>? members = null;
                bool hasMembers = invocation.SelectionMembers is not null &&
                    invocation.SelectionMembers.Any(entry =>
                    {
                        if (!entry.Key.Equals(definition.Name, StringComparison.OrdinalIgnoreCase)) return false;
                        members = entry.Value;
                        return true;
                    });
                if (all && (!value.Equals("all", StringComparison.OrdinalIgnoreCase) || hasMembers))
                    return CommandOutcome.Failure($"Inconsistent selection metadata for {definition.Name}.", CommandErrorKind.Validation);
                if (!all)
                {
                    if (!CommandTextParser.TrySplitCommaList(value, out var parsedMembers))
                        return CommandOutcome.Failure($"Invalid comma-separated {definition.Name}.", CommandErrorKind.Validation);
                    if (hasMembers && !parsedMembers.SequenceEqual(members!, StringComparer.Ordinal))
                        return CommandOutcome.Failure($"Inconsistent selection metadata for {definition.Name}.", CommandErrorKind.Validation);
                    if (value.Equals("all", StringComparison.OrdinalIgnoreCase))
                        return CommandOutcome.Failure($"Reserved all requires explicit selection metadata.", CommandErrorKind.Validation);
                    if (!hasMembers && value.Contains('"'))
                        return CommandOutcome.Failure($"Quoted selection requires explicit member metadata for {definition.Name}.", CommandErrorKind.Validation);
                    if (CommandTextParser.HasUnquotedAllMember(value) && parsedMembers.Count > 1)
                        return CommandOutcome.Failure("Reserved all must be used alone or quoted as a literal member.", CommandErrorKind.Validation);
                }
            }
            if (definition.CatalogProvider is not null)
            {
                var catalog = definition.CatalogProvider();
                if (catalog is null)
                    return CommandOutcome.Failure($"Catalog unavailable for {definition.Name}.", CommandErrorKind.Unavailable);
                bool all = invocation.AllSelections?.Contains(definition.Name, StringComparer.OrdinalIgnoreCase) ?? false;
                if (!all)
                {
                    IReadOnlyList<string> names;
                    if (definition.CommaSeparated)
                    {
                        var supplied = invocation.SelectionMembers?.FirstOrDefault(entry =>
                            entry.Key.Equals(definition.Name, StringComparison.OrdinalIgnoreCase)).Value;
                        if (supplied is not null)
                            names = supplied;
                        else if (!CommandTextParser.TrySplitCommaList(value, out names))
                            return CommandOutcome.Failure($"Invalid comma-separated {definition.Name}.", CommandErrorKind.Validation);
                    }
                    else names = new[] { value };
                    foreach (string name in names)
                    {
                        if (!catalog.Contains(name, StringComparer.OrdinalIgnoreCase))
                            return CommandOutcome.Failure($"Unknown {definition.Name}: {name}.", CommandErrorKind.Validation);
                    }
                }
            }
            if (definition.Validator?.Invoke(value) is { } error)
                return CommandOutcome.Failure(error, CommandErrorKind.Validation);
        }
        return operation.Handler(invocation);
    }

    /// <summary>Registers a complete reusable preset capability for one domain.</summary>
    public void RegisterPresets(string domain, PresetHandlers handlers, Func<IReadOnlyList<string>?>? presetNames = null)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        if (!domains.ContainsKey(domain)) throw new InvalidOperationException($"Unregistered domain: {domain}");
        var definitions = new[]
        {
            new OperationDefinition("save", domain, "Save current configuration as a preset.",
                new[] { new ArgumentDefinition("name", "Preset name", Completion: ArgumentCompletion.FreeForm) },
                Variant: "preset", IsPresetOperation: true, Handler: handlers.Save),
            new OperationDefinition("load", domain, "Replace current configuration from a preset.",
                new[] { new ArgumentDefinition("name", "Preset name", Completion: ArgumentCompletion.Preset, CatalogProvider: presetNames) },
                Variant: "preset", IsPresetOperation: true, Handler: handlers.Load),
            new OperationDefinition("list", domain, "List saved presets.", Array.Empty<ArgumentDefinition>(),
                Variant: "presets", IsPresetOperation: true, Handler: handlers.List),
            new OperationDefinition("inspect", domain, "Inspect a saved preset.",
                new[] { new ArgumentDefinition("name", "Preset name", Completion: ArgumentCompletion.Preset, CatalogProvider: presetNames) },
                Variant: "preset", IsPresetOperation: true, Handler: handlers.Inspect),
            new OperationDefinition("delete", domain, "Delete a saved preset.",
                new[] { new ArgumentDefinition("name", "Preset name", Completion: ArgumentCompletion.Preset, CatalogProvider: presetNames) },
                Variant: "preset", IsPresetOperation: true, Handler: handlers.Delete)
        };
        // Preflight the entire capability before mutating this registry.
        var staged = new CommandGrammar();
        foreach (var registeredDomain in domains.Values) staged.RegisterDomain(registeredDomain);
        foreach (var existing in operations.Values) staged.Register(existing);
        foreach (var definition in definitions) staged.Register(definition);
        foreach (var definition in definitions) Register(definition);
    }

    public IReadOnlyList<OperationDefinition> ForDomain(string domain) =>
        Operations.Where(o => o.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase)).ToArray();

    public IReadOnlyList<OperationDefinition> ForVerb(string verb) =>
        Operations.Where(o => o.Verb.Equals(verb, StringComparison.OrdinalIgnoreCase)).ToArray();

    private static string Key(string verb, string domain, string? qualifier, string? variant) =>
        $"{verb.Trim().ToLowerInvariant()}|{domain.Trim().ToLowerInvariant()}|{(qualifier ?? "").Trim().ToLowerInvariant()}|{(variant ?? "").Trim().ToLowerInvariant()}";

    private static void ValidateName(string name, string parameter)
    {
        if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name.Contains("  "))
            throw new ArgumentException("Names must be nonempty and canonically spaced.", parameter);
    }
}

public sealed record DomainDefinition(string Name, string Description);

public sealed record ArgumentDefinition(
    string Name,
    string Description,
    bool Required = true,
    ArgumentCompletion Completion = ArgumentCompletion.None,
    IReadOnlyList<string>? AllowedValues = null,
    bool CommaSeparated = false,
    Func<string, string?>? Validator = null,
    Func<IReadOnlyList<string>?>? CatalogProvider = null,
    string? DefaultDescription = null,
    Func<IReadOnlyList<string>?>? CompletionProvider = null);

public enum ArgumentCompletion { None, Literal, Catalog, Preset, FreeForm }

public sealed record OperationDefinition(
    string Verb,
    string Domain,
    string Description,
    IReadOnlyList<ArgumentDefinition> Arguments,
    string? Qualifier = null,
    bool IsPresetOperation = false,
    string? Variant = null,
    Func<CommandInvocation, CommandOutcome>? Handler = null,
    IReadOnlyList<string>? Examples = null,
    string? PersistenceDescription = null,
    string? DefaultDescription = null)
{
    public IReadOnlyList<string> Qualifiers => Qualifier is null ? Array.Empty<string>() : new[] { Qualifier };

    /// <summary>Canonical syntax derived from the same descriptors used by the parser.</summary>
    public string Syntax
    {
        get
        {
            var parts = new List<string> { Verb };
            if (Qualifier is not null) parts.Add(Qualifier);
            parts.Add(Domain);
            if (IsPresetOperation)
            {
                string? marker = Verb.ToLowerInvariant() switch
                {
                    "save" => "as",
                    "list" => "presets",
                    "inspect" or "delete" => "preset",
                    _ => null
                };
                if (marker is not null) parts.Add(marker);
            }
            foreach (var argument in Arguments)
            {
                string name = argument.AllowedValues is { Count: > 0 }
                    ? string.Join("|", argument.AllowedValues)
                    : argument.CommaSeparated ? $"{argument.Name}|all" : argument.Name;
                parts.Add(argument.Required ? $"<{name}>" : $"[{name}]");
            }
            return string.Join(" ", parts);
        }
    }
}


public sealed record CommandInvocation(
    string Verb, string Domain, IReadOnlyList<string> Arguments,
    string? Qualifier = null, string? Variant = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? SelectionMembers = null,
    IReadOnlyCollection<string>? AllSelections = null);

public sealed record CommandOutcome(bool Success, string Message)
{
    public CommandResultDetails Details { get; init; } = new();

    public static CommandOutcome Ok(string message) => new(true, message);
    public static CommandOutcome Failure(string message) => new(false, message);

    public static CommandOutcome Ok(string message, CommandResultDetails details) =>
        new(true, message) { Details = details ?? throw new ArgumentNullException(nameof(details)) };

    public static CommandOutcome Failure(string message, CommandErrorKind error) =>
        new(false, message) { Details = new(error) };
}

public sealed record PresetHandlers(
    Func<CommandInvocation, CommandOutcome> Save,
    Func<CommandInvocation, CommandOutcome> Load,
    Func<CommandInvocation, CommandOutcome> List,
    Func<CommandInvocation, CommandOutcome> Inspect,
    Func<CommandInvocation, CommandOutcome> Delete);
