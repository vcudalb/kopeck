namespace Kopeck.Contracts;

/// <summary>A category's year: totals plus 12 monthly values (January first).</summary>
public record CategoryYearDto(
    Guid CategoryId, string Name,
    decimal Spent, decimal Planned,
    IReadOnlyList<decimal> MonthlySpent, IReadOnlyList<decimal> MonthlyPlanned);
