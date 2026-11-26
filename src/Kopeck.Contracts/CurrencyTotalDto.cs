namespace Kopeck.Contracts;

/// <summary>One currency's month: amounts in different currencies are never added together.</summary>
/// <param name="Spent">Expenses are always in the budget currency, so this is 0 for every other currency.</param>
/// <param name="Left">Income - Spent - Saved.</param>
public record CurrencyTotalDto(string Currency, decimal Income, decimal Spent, decimal Saved, decimal Left);
