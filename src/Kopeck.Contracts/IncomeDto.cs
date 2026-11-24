namespace Kopeck.Contracts;

public record IncomeDto(Guid Id, Guid StreamId, string StreamName, DateOnly Date, decimal Amount, string? Note);
