namespace Kopeck.Contracts;

public record CategorySummaryDto(
    Guid CategoryId, string Name, string Color,
    decimal Planned, decimal Spent,
    decimal Left,      // Planned - Spent (negative when overspent)
    decimal OverBy,    // how much more than planned was spent (0 if not over)
    decimal PercentUsed,
    CategoryStatus Status,
    bool IsStarred = false);   // the caller's own star; starred rows come first
