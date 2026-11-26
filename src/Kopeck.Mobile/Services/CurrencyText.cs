using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

/// <summary>Formatting for amounts that may be in several currencies (they are never added together).</summary>
public static class CurrencyText
{
    public static string Format(decimal amount, string currency) => $"{amount:N2} {currency}";

    /// <summary>"1,200.00 EUR · 300.00 USD", the budget currency first; "0.00 EUR" when empty.</summary>
    public static string Join(IEnumerable<(string Currency, decimal Amount)> amounts, string budgetCurrency)
    {
        var parts = amounts.GroupBy(a => a.Currency)
            .Select(g => (Currency: g.Key, Amount: g.Sum(a => a.Amount)))
            .OrderByDescending(a => a.Currency == budgetCurrency).ThenBy(a => a.Currency)
            .Select(a => Format(a.Amount, a.Currency))
            .ToList();
        return parts.Count == 0 ? Format(0, budgetCurrency) : string.Join(" · ", parts);
    }

    /// <summary>The picker list with the budget currency first, marked as the default.</summary>
    public static IReadOnlyList<string> Choices(string budgetCurrency) =>
        [budgetCurrency, .. Currencies.Common.Where(c => c != budgetCurrency)];
}
