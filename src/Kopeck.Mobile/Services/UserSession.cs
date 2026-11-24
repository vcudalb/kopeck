using Kopeck.Contracts;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.Services;

/// <summary>
/// The JWT lives in secure storage (Keychain / Android Keystore), never in preferences. Platform storage is injected
/// (<see cref="ISecureStorage"/>, <see cref="IPreferences"/>) instead of using the static <c>.Default</c> instances, so this is testable.
/// </summary>
public sealed class UserSession(ISecureStorage secureStorage, IPreferences preferences, ILogger<UserSession> log) : IUserSession
{
    private const string TokenKey = "jwt";
    private const string EmailKey = "email";
    private const string NameKey = "name";

    public string? Token { get; private set; }
    public string Email { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public bool IsSignedIn => !string.IsNullOrEmpty(Token);

    public event Action? Expired;

    public async Task RestoreAsync()
    {
        try
        {
            Token = await secureStorage.GetAsync(TokenKey);
            Email = preferences.Get(EmailKey, "");
            DisplayName = preferences.Get(NameKey, "");
        }
        catch (Exception ex)   // the keystore can be unavailable after a device restore/backup
        {
            log.LogWarning(ex, "Could not read the stored session");
            Token = null;
        }
    }

    public async Task SignInAsync(AuthResponse auth)
    {
        (Token, Email, DisplayName) = (auth.Token, auth.Email, auth.DisplayName);
        preferences.Set(EmailKey, auth.Email);
        preferences.Set(NameKey, auth.DisplayName);
        await secureStorage.SetAsync(TokenKey, auth.Token);
    }

    public void SignOut()
    {
        (Token, Email, DisplayName) = (null, "", "");
        secureStorage.Remove(TokenKey);
        preferences.Remove(EmailKey);
        preferences.Remove(NameKey);
    }

    public void Expire()
    {
        SignOut();
        Expired?.Invoke();
    }
}
