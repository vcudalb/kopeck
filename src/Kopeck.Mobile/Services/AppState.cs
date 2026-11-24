using System.Globalization;
using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

/// <summary>State shared between screens: which budget and month are open.</summary>
public sealed class AppState(TimeProvider clock)
{
    public BudgetDto? Budget { get; set; }
    public DateOnly Month { get; set; } = FirstOfMonth(DateOnly.FromDateTime(clock.GetLocalNow().DateTime));
    public string PendingEmail { get; set; } = "";

    public bool CanEdit => Budget is { Role: >= BudgetRole.Editor };
    public bool IsOwner => Budget is { Role: BudgetRole.Owner };

    /// <summary>The open budget's id. Budget screens are only reachable after one was opened.</summary>
    public Guid BudgetId => Budget?.Id ?? throw new InvalidOperationException("No budget is open.");

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

    public void Reset() => (Budget, PendingEmail) = (null, "");

    private static DateOnly FirstOfMonth(DateOnly d) => new(d.Year, d.Month, 1);
}

public static class Money
{
    /// <summary>Accepts "12,50" and "12.50" regardless of the device's locale.</summary>
    public static bool TryParse(string? text, out decimal value) =>
        decimal.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
