namespace Kopeck.Contracts;

/// <param name="Amount">Signed: deposits are positive, withdrawals negative.</param>
/// <param name="CreatedAt">UTC.</param>
public record SavingsEntryDto(Guid Id, Guid PotId, DateOnly Date, decimal Amount, string? Note, string? CreatedBy, DateTime CreatedAt = default);
