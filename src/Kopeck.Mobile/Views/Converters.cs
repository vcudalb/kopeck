using System.Globalization;

namespace Kopeck.Mobile.Views;

/// <summary>true → false and back; for "show this when that is hidden" bindings.</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}
