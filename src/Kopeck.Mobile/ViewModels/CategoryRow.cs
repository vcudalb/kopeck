using Kopeck.Contracts;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Presentation of one category's planned-vs-real numbers.</summary>
public sealed class CategoryRow(CategorySummaryDto c, string currency)
{
    public Guid CategoryId => c.CategoryId;
    public string Name => c.Name;
    public bool IsStarred => c.IsStarred;

    /// <summary>"120.00 of 300.00 EUR" (or just the spent amount when there is no plan).</summary>
    public string SpentOfPlanText => c.Planned > 0 ? $"{c.Spent:N2} of {c.Planned:N2} {currency}" : $"{c.Spent:N2} {currency} spent";

    public string PercentText => c.Status == CategoryStatus.Unused ? "" : $"{c.PercentUsed:0}%";

    /// <summary>The key line: money left, or by how much the category is over its plan.</summary>
    public string LeftText => c.Status switch
    {
        CategoryStatus.Over => $"Over by {c.OverBy:N2}",
        CategoryStatus.Unplanned => "Not planned",
        CategoryStatus.Unused => "No plan, no spending",
        _ => $"{c.Left:N2} left",
    };

    public double Progress => Math.Min(1.0, (double)c.PercentUsed / 100.0);
    public Color StatusColor => StatusPalette.For(c.Status);
}
