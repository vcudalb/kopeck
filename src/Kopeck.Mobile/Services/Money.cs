using System.Globalization;

namespace Kopeck.Mobile.Services;

public static class Money
{
    /// <summary>Accepts "12,50" and "12.50" regardless of the device's locale.</summary>
    public static bool TryParse(string? text, out decimal value) =>
        decimal.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
}
