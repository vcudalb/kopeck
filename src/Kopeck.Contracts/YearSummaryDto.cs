namespace Kopeck.Contracts;

/// <summary>
/// One calendar year, for the yearly charts. Amounts are in the budget currency: income streams and savings pots in other
/// currencies are left out (they are never converted).
/// </summary>
/// <param name="Months">Always 12 rows, January first.</param>
/// <param name="Categories">Every category of the budget, by name; months in <see cref="CategoryYearDto"/> are January first.</param>
public record YearSummaryDto(int Year, string Currency, IReadOnlyList<MonthTotalsDto> Months, IReadOnlyList<CategoryYearDto> Categories);
