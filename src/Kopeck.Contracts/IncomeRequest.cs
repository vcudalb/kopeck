namespace Kopeck.Contracts;

public record IncomeRequest(Guid StreamId, DateOnly Date, decimal Amount, string? Note);
