using System.Globalization;
using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

/// <summary>State shared between screens: which budget and month are open.</summary>
public sealed class AppState
{
    public BudgetDto? Budget { get; set; }
    public DateOnly Month { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public string PendingEmail { get; set; } = "";

    public bool CanEdit => Budget is { Role: >= BudgetRole.Editor };
    public bool IsOwner => Budget is { Role: BudgetRole.Owner };

    public void Reset() => (Budget, PendingEmail) = (null, "");
}

public static class Money
{
    /// <summary>Accepts "12,50" and "12.50" regardless of the device's locale.</summary>
    public static bool TryParse(string? text, out decimal value) =>
        decimal.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
