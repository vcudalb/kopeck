namespace Kopeck.Contracts;

/// <summary>Saves several categories' planned amounts for one month in a single transaction (all or nothing).</summary>
public record SavePlansRequest(DateOnly Month, IReadOnlyList<PlanAmount> Items);
