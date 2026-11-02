namespace TowerDominion.Toolkit.Model;

/// <summary>Coordinates seed intent and runtime confirmation at seed generation time.</summary>
public sealed class SeedService : ISeedService
{
    private string? pendingSeed;
    private ModelOperation? pendingOperation;
    private ModelOperation? awaitingVerification;

    public ModelProperty<string> Seed { get; } = new(PropertyAuthority.Game);

    public string? PendingOverride => pendingSeed;

    public bool AwaitingVerification => awaitingVerification is not null;

    public bool ClearOverride()
    {
        if (pendingSeed is null)
        {
            return false;
        }

        pendingOperation?.Fail("Cancelled before application.");
        pendingSeed = null;
        pendingOperation = null;
        Seed.SetAuthority(PropertyAuthority.Game);
        return true;
    }

    public ModelOperation RequestOverride(string seed)
    {
        ModelOperation operation = new("game.run.seed.override");

        if (string.IsNullOrWhiteSpace(seed))
        {
            operation.Reject("Seed must not be empty.");
            return operation;
        }

        if (pendingOperation is not null
            && pendingOperation.Result.Status is OperationStatus.Requested or OperationStatus.Accepted)
        {
            pendingOperation.Fail("Superseded by a newer seed request.");
        }

        pendingSeed = seed;
        pendingOperation = operation;
        Seed.SetAuthority(PropertyAuthority.Toolkit);
        Seed.Request(seed);
        operation.Accept();
        return operation;
    }

    public void ApplyGeneratedSeed(string generatedSeed, Func<string, string> applyOverride)
    {
        ArgumentNullException.ThrowIfNull(applyOverride);
        awaitingVerification = null;
        Seed.Observe(generatedSeed);

        if (pendingSeed is null || pendingOperation is null)
        {
            Seed.SetAuthority(PropertyAuthority.Game);
            return;
        }

        try
        {
            string actualSeed = applyOverride(pendingSeed);
            Seed.ConfirmApplied(actualSeed);
            Seed.Observe(actualSeed);
            pendingOperation.Apply();
            awaitingVerification = pendingOperation;

            // An encoded seed may differ from its numeric input. The runtime's returned
            // encoded value is authoritative evidence of what was applied.
            // Applying and reading within the same callback is not independent
            // verification. A later game observation must confirm the result.
        }
        catch (Exception exception)
        {
            pendingOperation.Fail(exception.Message);
        }
        finally
        {
            pendingSeed = null;
            pendingOperation = null;
            Seed.SetAuthority(PropertyAuthority.Game);
        }
    }

    public void ObserveEffectiveSeed(string encodedSeed)
    {
        Seed.Observe(encodedSeed);
        if (awaitingVerification is not { } operation)
        {
            return;
        }

        awaitingVerification = null;
        if (string.Equals(encodedSeed, Seed.Snapshot.Applied, StringComparison.Ordinal))
        {
            Seed.Verify();
            operation.Verify();
        }
        else
        {
            operation.Fail($"Effective seed differs from applied seed: {encodedSeed}");
        }
    }
}
