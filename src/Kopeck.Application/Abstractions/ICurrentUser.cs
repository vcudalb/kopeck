namespace Kopeck.Application.Abstractions;

/// <summary>The signed-in caller. Implemented by the web layer from the JWT claims.</summary>
public interface ICurrentUser
{
    Guid Id { get; }

    /// <summary>Lower-cased, verified email.</summary>
    string Email { get; }

    string DisplayName { get; }
}
