using System.Globalization;

namespace Kopeck.Mobile.Services;

internal static class Routes
{
    public static string Budget(Guid id) => $"/api/budgets/{id}";
    public static string Month(DateOnly m) => m.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
