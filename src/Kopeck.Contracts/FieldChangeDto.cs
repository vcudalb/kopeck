namespace Kopeck.Contracts;

/// <summary>
/// One field before and after an action. Dates are yyyy-MM-dd, amounts have two decimals, a category is its name at
/// that moment. <paramref name="Old"/> is empty for an added entry, <paramref name="New"/> for a deleted one.
/// </summary>
public record FieldChangeDto(string Field, string? Old, string? New);
