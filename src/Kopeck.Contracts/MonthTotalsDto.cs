namespace Kopeck.Contracts;

/// <summary>One month of a <see cref="YearSummaryDto"/>, in the budget currency.</summary>
public record MonthTotalsDto(DateOnly Month, decimal Income, decimal Planned, decimal Spent, decimal Saved);
