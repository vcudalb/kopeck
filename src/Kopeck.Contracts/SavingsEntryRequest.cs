namespace Kopeck.Contracts;

/// <param name="Amount">Always positive; <paramref name="Kind"/> says which way the money moves.</param>
public record SavingsEntryRequest(SavingsEntryKind Kind, DateOnly Date, decimal Amount, string? Note);
