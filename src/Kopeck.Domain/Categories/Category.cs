using Kopeck.Domain.Common;

namespace Kopeck.Domain.Categories;

public sealed class Category : IBudgetOwned
{
    private Category() { }   // EF Core

    public Category(Guid budgetId, string name, string? color)
    {
        BudgetId = budgetId;
        Rename(name);
        Recolor(color);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public string Name { get; private set; } = "";
    public string Color { get; private set; } = DomainRules.DefaultCategoryColor;

    public void Rename(string name) => Name = name.Trim();

    /// <summary>Keeps the current colour when the new one is missing or invalid.</summary>
    public void Recolor(string? color) => Color = DomainRules.ValidColor(color) ?? Color;
}
