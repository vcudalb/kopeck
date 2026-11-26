namespace Kopeck.Contracts;

/// <summary>
/// <paramref name="CreatedAt"/> and <paramref name="UpdatedAt"/> are UTC; <paramref name="UpdatedAt"/> and
/// <paramref name="UpdatedBy"/> stay empty until the expense is first changed.
/// </summary>
public record ExpenseDto(
    Guid Id, Guid CategoryId, string CategoryName, DateOnly Date, decimal Amount, string? Note, string? CreatedBy,
    DateTime CreatedAt = default, DateTime? UpdatedAt = null, string? UpdatedBy = null);
