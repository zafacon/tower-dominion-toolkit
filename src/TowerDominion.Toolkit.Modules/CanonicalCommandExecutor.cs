namespace TowerDominion.Toolkit.Modules;

/// <summary>Single-grammar execution surface for command transports.</summary>
public sealed class CanonicalCommandExecutor
{
    private readonly CommandTextParser parser;

    public CanonicalCommandExecutor(CommandGrammar grammar)
    {
        parser = new CommandTextParser(grammar ?? throw new ArgumentNullException(nameof(grammar)));
    }

    public CommandOutcome ExecuteDetailed(string input) => parser.Execute(input);

    public string Execute(string input) => ExecuteDetailed(input).Message;
}
