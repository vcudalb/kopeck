namespace Kopeck.Contracts;

/// <summary><paramref name="IsStarred"/> is the caller's own star: lists put starred categories first.</summary>
public record CategoryDto(Guid Id, string Name, string Color, bool IsStarred = false);
