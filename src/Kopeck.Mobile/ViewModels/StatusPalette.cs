using Kopeck.Contracts;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Status colours in one place, so every screen speaks the same visual language.</summary>
internal static class StatusPalette
{
    public static readonly Color Over = Color.FromArgb("#C62828");
    public static readonly Color Warning = Color.FromArgb("#F9A825");
    public static readonly Color Unplanned = Color.FromArgb("#6A1B9A");
    public static readonly Color Unused = Color.FromArgb("#9E9E9E");
    public static readonly Color Good = Color.FromArgb("#2E7D32");

    public static Color For(CategoryStatus status) => status switch
    {
        CategoryStatus.Over => Over,
        CategoryStatus.Warning => Warning,
        CategoryStatus.Unplanned => Unplanned,
        CategoryStatus.Unused => Unused,
        _ => Good,
    };
}
