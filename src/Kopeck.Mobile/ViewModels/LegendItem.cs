namespace Kopeck.Mobile.ViewModels;

/// <summary>A chart legend entry: a filled swatch for a bar series, or a line for a marker series.</summary>
public sealed record LegendItem(string Name, Color Color, bool IsMarker)
{
    public bool IsBar => !IsMarker;
}
