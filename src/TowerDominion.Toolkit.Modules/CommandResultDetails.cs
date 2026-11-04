namespace TowerDominion.Toolkit.Modules;

/// <summary>Machine-readable failure categories, independent of presentation text.</summary>
public enum CommandErrorKind
{
    None,
    Syntax,
    Validation,
    Unavailable,
    Conflict,
    Persistence,
    Execution
}

/// <summary>One completed mutation, with optional before/after values.</summary>
public sealed record CommandStateChange(string Domain, string Property, object? Before, object? After);

/// <summary>Optional UI preview. The consumer decides how to render the payload.</summary>
public sealed record CommandPreview(string Kind, IReadOnlyDictionary<string, object?> Data);

/// <summary>Structured details supplement the legacy success/message response.</summary>
public sealed record CommandResultDetails(
    CommandErrorKind Error = CommandErrorKind.None,
    IReadOnlyDictionary<string, object?>? Data = null,
    IReadOnlyList<CommandStateChange>? Changes = null,
    CommandPreview? Preview = null);
