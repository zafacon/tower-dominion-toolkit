namespace TowerDominion.Toolkit.Model;

public enum OperationStatus
{
    Requested,
    Accepted,
    Rejected,
    Applied,
    Verified,
    Failed
}

public sealed record OperationResult(
    string OperationId,
    OperationStatus Status,
    string? Reason = null);

/// <summary>Tracks a request through explicit acceptance, application, and verification.</summary>
public sealed class ModelOperation
{
    public ModelOperation(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Operation identifier is required.", nameof(id));
        }

        Id = id;
    }

    public string Id { get; }

    public OperationResult Result { get; private set; } = new(string.Empty, OperationStatus.Requested);

    public void Accept() => Transition(OperationStatus.Accepted);

    public void Reject(string reason) => Transition(OperationStatus.Rejected, reason);

    public void Apply() => Transition(OperationStatus.Applied);

    public void Verify() => Transition(OperationStatus.Verified);

    public void Fail(string reason) => Transition(OperationStatus.Failed, reason);

    private void Transition(OperationStatus next, string? reason = null)
    {
        bool allowed = (Result.Status, next) switch
        {
            (OperationStatus.Requested, OperationStatus.Accepted or OperationStatus.Rejected or OperationStatus.Failed) => true,
            (OperationStatus.Accepted, OperationStatus.Applied or OperationStatus.Failed) => true,
            (OperationStatus.Applied, OperationStatus.Verified or OperationStatus.Failed) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Invalid operation transition: {Result.Status} to {next}.");
        }

        Result = new(Id, next, reason);
    }
}
