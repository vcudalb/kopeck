namespace Kopeck.Domain.Common;

/// <summary>Anything that belongs to one budget (and is deleted with it).</summary>
public interface IBudgetOwned
{
    Guid BudgetId { get; }
}
