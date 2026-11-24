namespace Kopeck.Domain.Common;

public static class MonthExtensions
{
    /// <summary>Months are always identified by their first day.</summary>
    public static DateOnly FirstOfMonth(this DateOnly date) => new(date.Year, date.Month, 1);
}
