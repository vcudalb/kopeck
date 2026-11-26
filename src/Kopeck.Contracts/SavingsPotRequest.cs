namespace Kopeck.Contracts;

/// <param name="Currency">Optional; the budget's currency when left out. Can only change while the pot is empty.</param>
/// <param name="Target">Optional goal; null or 0 means no goal.</param>
public record SavingsPotRequest(string Name, string? Color = null, string? Currency = null, decimal? Target = null);
