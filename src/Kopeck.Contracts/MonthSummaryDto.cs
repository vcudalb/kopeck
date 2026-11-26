namespace Kopeck.Contracts;

/// <summary>
/// The month at a glance. The single-value totals are in the budget currency; <see cref="Totals"/> has one row per
/// currency used this month (budget currency first), because amounts in different currencies are never converted.
/// </summary>
public record MonthSummaryDto(
    DateOnly Month, string Currency,
    decimal TotalIncome, decimal TotalPlanned, decimal TotalSpent,
    decimal TotalOverspend,   // sum of OverBy across categories
    decimal Unallocated,      // TotalIncome - TotalPlanned
    decimal NetLeft,          // TotalIncome - TotalSpent - TotalSaved
    IReadOnlyList<CategorySummaryDto> Categories,
    IReadOnlyList<IncomeStreamSummaryDto> IncomeStreams,
    decimal TotalSaved,
    IReadOnlyList<SavingsSummaryDto> Savings,
    IReadOnlyList<CurrencyTotalDto> Totals);
