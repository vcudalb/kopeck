namespace Kopeck.Domain.History;

/// <summary>
/// One field of an entry before and after a change, as display-ready text (dates as yyyy-MM-dd, amounts with two
/// decimals, a category by its name at that moment). <see cref="Old"/> is empty when the entry was added, <see cref="New"/>
/// when it was deleted.
/// </summary>
public sealed record FieldChange(string Field, string? Old, string? New);
