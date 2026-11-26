namespace Kopeck.Contracts;

/// <param name="Currency">Optional; the budget's currency when left out.</param>
public record IncomeStreamRequest(string Name, string? Currency = null);
