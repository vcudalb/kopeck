using Kopeck.Domain.Planning;

namespace Kopeck.Api.Tests;

public class PlanVsActualTests
{
    [Theory]
    [InlineData(0, 0, SpendingStatus.Unused)]
    [InlineData(0, 10, SpendingStatus.Unplanned)]
    [InlineData(100, 50, SpendingStatus.OnTrack)]
    [InlineData(100, 80, SpendingStatus.Warning)]
    [InlineData(100, 100, SpendingStatus.Warning)]
    [InlineData(100, 100.01, SpendingStatus.Over)]
    public void Maps_planned_and_spent_to_a_status(decimal planned, decimal spent, SpendingStatus expected) =>
        Assert.Equal(expected, new PlanVsActual(planned, spent).Status);

    [Fact]
    public void Left_is_negative_and_over_by_positive_when_overspent()
    {
        var line = new PlanVsActual(Planned: 3000, Spent: 3500);
        Assert.Equal((-500m, 500m, 116.7m), (line.Left, line.OverBy, line.PercentUsed));
    }

    [Fact]
    public void Unplanned_spending_counts_fully_as_over()
    {
        var line = new PlanVsActual(Planned: 0, Spent: 250);
        Assert.Equal((250m, 100m), (line.OverBy, line.PercentUsed));
    }
}
