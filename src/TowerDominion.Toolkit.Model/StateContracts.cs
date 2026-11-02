namespace TowerDominion.Toolkit.Model;

/// <summary>Identifies which side currently determines a property's effective value.</summary>
public enum PropertyAuthority
{
    Game,
    Toolkit
}

/// <summary>Separates observations from toolkit intent and confirmed application.</summary>
public sealed record PropertySnapshot<T>(
    T? Observed,
    T? Desired,
    T? Applied,
    PropertyAuthority Authority,
    bool IsVerified);

/// <summary>Tracks an individual property without treating a request as a confirmed game change.</summary>
public sealed class ModelProperty<T>
{
    private T? observed;
    private T? desired;
    private T? applied;
    private bool verified;

    public ModelProperty(PropertyAuthority authority)
    {
        Authority = authority;
    }

    public PropertyAuthority Authority { get; private set; }

    public PropertySnapshot<T> Snapshot => new(observed, desired, applied, Authority, verified);

    public void SetAuthority(PropertyAuthority authority)
    {
        Authority = authority;
        verified = false;
    }

    public void Request(T? value)
    {
        desired = value;
        verified = false;
    }

    public void Observe(T? value)
    {
        observed = value;
        verified = false;
    }

    public void ConfirmApplied(T? value)
    {
        applied = value;
        verified = false;
    }

    public void Verify()
    {
        if (!EqualityComparer<T?>.Default.Equals(observed, applied))
        {
            throw new InvalidOperationException("Observed and applied values do not match.");
        }

        verified = true;
    }
}
