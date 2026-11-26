namespace Kopeck.Contracts;

/// <param name="Currency">The stream's currency.</param>
/// <param name="CreatedAt">UTC.</param>
public record IncomeDto(
    Guid Id, Guid StreamId, string StreamName, DateOnly Date, decimal Amount, string? Note, string Currency,
    string? CreatedBy = null, DateTime CreatedAt = default);
