namespace Kopeck.Contracts;

/// <summary>One action in an entry's history: who, when (UTC) and which values were added or changed.</summary>
public record EntryChangeDto(EntryAction Action, DateTime At, string By, IReadOnlyList<FieldChangeDto> Changes);
