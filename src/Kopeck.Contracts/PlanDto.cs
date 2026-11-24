namespace Kopeck.Contracts;

/// <summary>The planned expense for one category in one month.</summary>
public record PlanDto(Guid CategoryId, string CategoryName, decimal Amount);
