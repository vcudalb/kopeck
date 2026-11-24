namespace Kopeck.Contracts;

public record ExpenseDto(Guid Id, Guid CategoryId, string CategoryName, DateOnly Date, decimal Amount, string? Note, string? CreatedBy);
