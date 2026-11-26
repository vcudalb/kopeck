namespace Kopeck.Domain.Common;

/// <summary>Who did something: the user's id and their name at that moment (kept even if they later rename or leave).</summary>
public sealed record Actor(Guid UserId, string Name);
