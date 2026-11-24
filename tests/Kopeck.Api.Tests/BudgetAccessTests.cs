using Kopeck.Domain.Budgets;

namespace Kopeck.Api.Tests;

public class BudgetAccessTests
{
    [Fact]
    public void Owner_share_and_stranger_get_the_right_access()
    {
        var owner = Guid.NewGuid();
        var budget = new Budget("Home", "EUR", owner);
        var share = new BudgetShare(budget.Id, "friend@test.dev", AccessLevel.Editor);
        var otherBudgetShare = new BudgetShare(Guid.NewGuid(), "friend@test.dev", AccessLevel.Editor);

        Assert.Equal(AccessLevel.Owner, budget.AccessFor(owner, null));
        Assert.Equal(AccessLevel.Editor, budget.AccessFor(Guid.NewGuid(), share));
        Assert.Null(budget.AccessFor(Guid.NewGuid(), otherBudgetShare));
        Assert.Null(budget.AccessFor(Guid.NewGuid(), null));
    }

    [Fact]
    public void Ownership_cannot_be_shared() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BudgetShare(Guid.NewGuid(), "x@test.dev", AccessLevel.Owner));
}
