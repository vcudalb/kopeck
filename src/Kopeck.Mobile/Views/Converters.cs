using System.Globalization;

namespace Kopeck.Mobile.Views;

/// <summary>true → false and back; for "show this when that is hidden" bindings.</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

/// <summary>A category's name for pickers, with a star in front when the person starred it.</summary>
public sealed class CategoryLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Kopeck.Contracts.CategoryDto c ? (c.IsStarred ? "★ " + c.Name : c.Name) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
