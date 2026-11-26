using Kopeck.Mobile.Views.Charts;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// Gives each category its chart colour from its position in the budget's name order, so a category has the same
/// colour on the Overview and on the year screen whatever its rank that month.
/// </summary>
public sealed class CategoryColors
{
    private readonly Dictionary<Guid, int> _index;

    public CategoryColors(IEnumerable<(Guid Id, string Name)> categories) =>
        _index = categories.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select((c, i) => (c.Id, i)).ToDictionary(x => x.Id, x => x.i);

    public Color For(Guid categoryId) => ChartPalette.ForIndex(_index.GetValueOrDefault(categoryId, int.MaxValue));

    /// <summary>Past the eighth category every one shares the neutral colour (hues are never repeated).</summary>
    public bool HasOwnColor(Guid categoryId) => _index.GetValueOrDefault(categoryId, int.MaxValue) < ChartPalette.SlotCount;
}
