namespace Kopeck.Contracts;

public record PlanRequest(Guid CategoryId, DateOnly Month, decimal Amount);
