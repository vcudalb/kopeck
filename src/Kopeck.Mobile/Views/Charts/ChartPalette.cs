namespace Kopeck.Mobile.Views.Charts;

/// <summary>
/// Chart colours. The categorical order is fixed (it is what keeps neighbouring colours apart for colour-blind readers)
/// and was validated against the app's card surface in both themes: light #FFFFFF and dark #1C201D. Three light colours
/// sit below 3:1 contrast, so every chart in the app also shows names and amounts as text.
/// A category keeps its colour everywhere (its position by name), never its rank: categories past the eighth share the
/// neutral "other" colour instead of repeating a hue.
/// </summary>
public static class ChartPalette
{
    private static readonly Color[] Light = Hex("#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300", "#6250d6", "#e34948");

    private static readonly Color[] Dark = Hex("#3987e5", "#d95926", "#199e70", "#c98500", "#d55181", "#0ca30c", "#9085e9", "#e66767");

    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    public static int SlotCount => Light.Length;

    /// <summary>The colour of the category at <paramref name="index"/> in the budget's name order.</summary>
    public static Color ForIndex(int index) => index >= 0 && index < SlotCount ? (IsDark ? Dark : Light)[index] : Other;

    public static Color Series1 => ForIndex(0);
    public static Color Series2 => ForIndex(1);

    public static Color Other => IsDark ? Color.FromArgb("#6B736D") : Color.FromArgb("#9AA39C");
    public static Color Surface => IsDark ? Color.FromArgb("#1C201D") : Colors.White;
    public static Color Grid => IsDark ? Color.FromArgb("#2F3530") : Color.FromArgb("#E9ECEA");
    public static Color Text => IsDark ? Color.FromArgb("#9AA39C") : Color.FromArgb("#69716B");
    public static Color Highlight => IsDark ? Color.FromArgb("#262B27") : Color.FromArgb("#F1F4F2");
    private static Color[] Hex(params string[] values) => values.Select(Color.FromArgb).ToArray();

    public static Color Marker => IsDark ? Color.FromArgb("#E6E9E7") : Color.FromArgb("#1A1D1B");
}
