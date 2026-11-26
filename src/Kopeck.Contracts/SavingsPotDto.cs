namespace Kopeck.Contracts;

/// <param name="Target">The goal, or null when the pot has none.</param>
/// <param name="Balance">Deposits minus withdrawals, all time.</param>
public record SavingsPotDto(Guid Id, string Name, string Color, string Currency, decimal? Target, decimal Balance);
