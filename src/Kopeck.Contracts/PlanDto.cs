namespace Kopeck.Contracts;

/// <summary>The planned expense for one category in one month. Starred categories (the caller's own) come first.</summary>
public record PlanDto(Guid CategoryId, string CategoryName, decimal Amount, bool IsStarred = false);
