namespace Kopeck.Contracts;

/// <summary>A budget is the dashboard that can be shared; <see cref="Role"/> is the caller's access to it.</summary>
public record BudgetDto(Guid Id, string Name, string Currency, BudgetRole Role, string OwnerEmail);
