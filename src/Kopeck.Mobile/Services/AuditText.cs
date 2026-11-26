using System.Globalization;

namespace Kopeck.Mobile.Services;

/// <summary>"Added by Alice · 26 Nov 2026, 13:34": who did something and when, in the phone's time zone.</summary>
public static class AuditText
{
    /// <summary>
    /// Entries added before times were kept were given midnight UTC of their booked date; for those only the date is
    /// shown, since the time would be made up.
    /// </summary>
    public static string When(DateTime utc) => utc.TimeOfDay == TimeSpan.Zero
        ? utc.ToString("d MMM yyyy", CultureInfo.CurrentCulture)
        : DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("d MMM yyyy, HH:mm", CultureInfo.CurrentCulture);

    public static string By(string? name, DateTime utc) => $"{(string.IsNullOrWhiteSpace(name) ? "someone" : name)} · {When(utc)}";
}
