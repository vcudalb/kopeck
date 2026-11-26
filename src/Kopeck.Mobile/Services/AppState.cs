using System.Globalization;
using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

/// <summary>State shared between screens: which budget, month and savings pot are open.</summary>
public sealed class AppState(TimeProvider clock)
{
    public BudgetDto? Budget { get; set; }
    public DateOnly Month { get; set; } = FirstOfMonth(DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
    public string PendingEmail { get; set; } = "";

    /// <summary>The pot shown on the pot screen (set by the Savings tab before navigating).</summary>
    public SavingsPotDto? Pot { get; set; }

    /// <summary>Set by the Overview's "+" on a category: the Expenses tab opens a new expense in it, then clears this.</summary>
    public Guid? QuickAddCategoryId { get; set; }

    /// <summary>The expense whose history the history screen shows (set by the Expenses tab before navigating).</summary>
    public ExpenseDto? HistoryExpense { get; set; }

    public bool CanEdit => Budget is { Role: >= BudgetRole.Editor };
    public bool IsOwner => Budget is { Role: BudgetRole.Owner };

    /// <summary>The open budget's id. Budget screens are only reachable after one was opened.</summary>
    public Guid BudgetId => Budget?.Id ?? throw new InvalidOperationException("No budget is open.");

    /// <summary>The budget's default currency: new income streams and savings pots start in it.</summary>
    public string Currency => Budget?.Currency ?? Currencies.Default;

    public string MonthText => Month.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    /// <summary>Today when it falls in the open month, otherwise the month's first day (so new entries land in the month on screen).</summary>
    public DateTime DefaultEntryDate
    {
        get
        {
            var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
            return (FirstOfMonth(today) == Month ? today : Month).ToDateTime(TimeOnly.MinValue);
        }
    }

    /// <summary>Today, for entries that are not tied to the month on screen (savings).</summary>
    public DateTime Today => clock.GetLocalNow().Date;

    public void Reset() => (Budget, PendingEmail, Pot, QuickAddCategoryId, HistoryExpense) = (null, "", null, null, null);

    private static DateOnly FirstOfMonth(DateOnly d) => new(d.Year, d.Month, 1);
}
