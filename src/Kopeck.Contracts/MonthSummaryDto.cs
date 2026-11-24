namespace Kopeck.Contracts;

public record MonthSummaryDto(
    DateOnly Month, string Currency,
    decimal TotalIncome, decimal TotalPlanned, decimal TotalSpent,
    decimal TotalOverspend,   // sum of OverBy across categories
    decimal Unallocated,      // TotalIncome - TotalPlanned
    decimal NetLeft,          // TotalIncome - TotalSpent
    IReadOnlyList<CategorySummaryDto> Categories,
    IReadOnlyList<IncomeStreamSummaryDto> IncomeStreams);
