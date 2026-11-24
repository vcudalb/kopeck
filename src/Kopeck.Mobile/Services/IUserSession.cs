using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

/// <summary>The signed-in user, as the rest of the app sees it.</summary>
public interface IUserSession
{
    string? Token { get; }
    string Email { get; }
    string DisplayName { get; }
    bool IsSignedIn { get; }

    /// <summary>Raised when the server rejects the token (expired or revoked). The app returns to the sign-in screen.</summary>
    event Action? Expired;

    Task RestoreAsync();
    Task SignInAsync(AuthResponse auth);
    void SignOut();
    void Expire();
}
