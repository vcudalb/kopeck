namespace Kopeck.Contracts;

public record IncomeStreamSummaryDto(Guid StreamId, string Name, decimal Amount, string Currency);
