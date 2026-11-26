using Kopeck.Domain.Common;

namespace Kopeck.Domain.Savings;

/// <summary>
/// Money set aside for a purpose, such as "Emergency fund" or "Holiday". A pot has its own currency (it may differ from
/// the budget's) and an optional target. Its balance is the sum of its <see cref="SavingsEntry"/> rows.
/// </summary>
public sealed class SavingsPot : IBudgetOwned
{
    private SavingsPot() { }   // EF Core

    public SavingsPot(Guid budgetId, string name, string? color, string currency, decimal? target)
    {
        BudgetId = budgetId;
        Currency = currency;
        Rename(name);
        Recolor(color);
        ChangeTarget(target);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BudgetId { get; private set; }
    public string Name { get; private set; } = "";
    public string Color { get; private set; } = DomainRules.DefaultCategoryColor;
    public string Currency { get; private set; } = "";

    /// <summary>The amount the user is saving towards; null when the pot has no goal.</summary>
    public decimal? Target { get; private set; }

    public void Rename(string name) => Name = name.Trim();

    /// <summary>Keeps the current colour when the new one is missing or invalid.</summary>
    public void Recolor(string? color) => Color = DomainRules.ValidColor(color) ?? Color;

    /// <summary>0 or null removes the goal.</summary>
    public void ChangeTarget(decimal? target) => Target = target is > 0 ? target : null;

    /// <summary>Only allowed while the pot is empty, so existing entries never change meaning.</summary>
    public void ChangeCurrency(string currency) => Currency = currency;
}
