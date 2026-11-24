namespace Kopeck.Contracts;

public record ExpenseRequest(Guid CategoryId, DateOnly Date, decimal Amount, string? Note);
