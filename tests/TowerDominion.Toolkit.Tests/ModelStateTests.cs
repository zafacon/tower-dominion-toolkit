using TowerDominion.Toolkit.Model;

namespace TowerDominion.Toolkit.Tests;

public sealed class ModelStateTests
{
    [Fact]
    public void RequestedValueIsNotAppliedOrVerified()
    {
        ModelProperty<string> property = new(PropertyAuthority.Toolkit);
        property.Request("42");

        Assert.Equal("42", property.Snapshot.Desired);
        Assert.Null(property.Snapshot.Applied);
        Assert.False(property.Snapshot.IsVerified);
    }

    [Fact]
    public void VerificationRequiresMatchingObservation()
    {
        ModelProperty<int> property = new(PropertyAuthority.Toolkit);
        property.Request(42);
        property.ConfirmApplied(42);
        property.Observe(41);

        Assert.Throws<InvalidOperationException>(() => property.Verify());

        property.Observe(42);
        property.Verify();
        Assert.True(property.Snapshot.IsVerified);
    }

    [Fact]
    public void AuthorityChangeInvalidatesVerification()
    {
        ModelProperty<int> property = new(PropertyAuthority.Game);
        property.ConfirmApplied(5);
        property.Observe(5);
        property.Verify();

        property.SetAuthority(PropertyAuthority.Toolkit);

        Assert.Equal(PropertyAuthority.Toolkit, property.Snapshot.Authority);
        Assert.False(property.Snapshot.IsVerified);
    }

    [Fact]
    public void OperationProgressesThroughDistinctStates()
    {
        ModelOperation operation = new("seed.override");
        Assert.Equal(OperationStatus.Requested, operation.Result.Status);

        operation.Accept();
        Assert.Equal(OperationStatus.Accepted, operation.Result.Status);
        operation.Apply();
        Assert.Equal(OperationStatus.Applied, operation.Result.Status);
        operation.Verify();
        Assert.Equal(OperationStatus.Verified, operation.Result.Status);
    }

    [Fact]
    public void OperationCannotSkipAcceptance()
    {
        ModelOperation operation = new("seed.override");
        Assert.Throws<InvalidOperationException>(() => operation.Apply());
    }

    [Fact]
    public void RejectedOperationCannotBeApplied()
    {
        ModelOperation operation = new("seed.override");
        operation.Reject("Not available");
        Assert.Equal("Not available", operation.Result.Reason);
        Assert.Throws<InvalidOperationException>(() => operation.Apply());
    }
}
