namespace Kopeck.Contracts;

public record BudgetDto(Guid Id, string Name, string Currency, BudgetRole Role, string OwnerEmail);
