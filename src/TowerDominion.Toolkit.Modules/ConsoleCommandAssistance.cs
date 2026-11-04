namespace TowerDominion.Toolkit.Modules;

/// <summary>Registry-derived console help and cursor-aware completion.</summary>
public sealed class ConsoleCommandAssistance
{
    private readonly CommandStructuralCompletion structural;
    private readonly CommandArgumentCompletion arguments;
    private readonly CommandHelp help;

    public ConsoleCommandAssistance(CommandGrammar grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        structural = new CommandStructuralCompletion(grammar);
        arguments = new CommandArgumentCompletion(grammar);
        help = new CommandHelp(grammar);
    }

    public CommandCompletionResult Complete(string input, int cursor)
    {
        var argument = arguments.Complete(input, cursor);
        return argument.Status == ArgumentCompletionStatus.NotArgument
            ? structural.Complete(input, cursor)
            : argument.Completion;
    }

    public string Help(string? target)
    {
        var result = help.Query(target);
        if (!result.Success) return result.Title;
        return "Canonical command reference:" + Environment.NewLine
            + string.Join(Environment.NewLine, result.Syntax.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
