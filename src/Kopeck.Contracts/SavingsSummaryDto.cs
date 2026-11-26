namespace Kopeck.Contracts;

/// <param name="Amount">Net amount put into the pot during the month (deposits minus withdrawals).</param>
public record SavingsSummaryDto(Guid PotId, string Name, string Color, decimal Amount, string Currency);
