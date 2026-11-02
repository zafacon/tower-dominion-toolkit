namespace TowerDominion.Toolkit.Modules;

/// <summary>Transport-neutral command routing shared by file commands and future console UI.</summary>
public sealed record ToolkitCommand(string Name, string Usage, string Summary, Func<string, string> Execute);

public sealed class CommandRegistry
{
    private readonly Dictionary<string, ToolkitCommand> commands = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ToolkitCommand> Commands => commands.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    public void Register(ToolkitCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Any(char.IsWhiteSpace))
            throw new ArgumentException("Command name must be one word.", nameof(command));
        if (!commands.TryAdd(command.Name, command))
            throw new InvalidOperationException($"Command '{command.Name}' is already registered.");
    }

    private readonly Dictionary<string, List<(string Usage, string Summary)>> details = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(string Usage, string Summary)>> topics = new(StringComparer.OrdinalIgnoreCase);

    public void AddHelp(string verb, string usage, string summary, params string[] topicNames)
    {
        if (!commands.ContainsKey(verb)) return;
        if (!details.TryGetValue(verb, out var entries)) details[verb] = entries = new();
        entries.Add((usage, summary));
        foreach (string topic in topicNames)
        {
            if (!topics.TryGetValue(topic, out var topicEntries)) topics[topic] = topicEntries = new();
            topicEntries.Add((usage, summary));
        }
    }

    private IEnumerable<string> Describe(string verb) =>
        details.TryGetValue(verb, out var entries)
            ? entries.Select(x => $"{x.Usage} — {x.Summary}")
            : commands.TryGetValue(verb, out var command)
                ? new[] { $"{command.Usage} — {command.Summary}" }
                : Array.Empty<string>();

    public string Execute(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Help();
        string[] parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length == 1) return Help();
            string topic = parts[1].Trim();
            if (topics.TryGetValue(topic, out var topicEntries))
                return string.Join(Environment.NewLine, topicEntries.Select(x => $"{x.Usage} — {x.Summary}").Distinct());
            return commands.ContainsKey(topic)
                ? string.Join(Environment.NewLine, Describe(topic))
                : $"Unknown command or help topic: {topic}";
        }

        return commands.TryGetValue(parts[0], out ToolkitCommand? command)
            ? command.Execute(command.Name + (parts.Length == 2 ? " " + parts[1] : string.Empty))
            : $"Unknown command: {parts[0]}. Type help for available commands.";
    }

    private string Help() => "Commands:" + Environment.NewLine
        + string.Join(Environment.NewLine, Commands.SelectMany(command => Describe(command.Name)))
        + (topics.Count == 0 ? "" : Environment.NewLine + "Topics: " + string.Join(", ", topics.Keys.OrderBy(x => x)));
}
