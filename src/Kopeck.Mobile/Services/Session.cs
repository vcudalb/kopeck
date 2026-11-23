using Kopeck.Contracts;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.Services;

/// <summary>Holds the signed-in user. The JWT lives in SecureStorage (Keychain / Android Keystore), never in Preferences.</summary>
public sealed class Session(ILogger<Session> log)
{
    private const string TokenKey = "jwt";

    public string? Token { get; private set; }
    public string Email { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public bool IsSignedIn => !string.IsNullOrEmpty(Token);

    /// <summary>Raised when the server rejects the token (expired or revoked). The app returns to the sign-in screen.</summary>
    public event Action? Expired;

    public async Task RestoreAsync()
    {
        try
        {
            Token = await SecureStorage.Default.GetAsync(TokenKey);
            Email = Preferences.Default.Get("email", "");
            DisplayName = Preferences.Default.Get("name", "");
        }
        catch (Exception ex)   // keystore can be unavailable after a device restore/backup
        {
            log.LogWarning(ex, "Could not read the stored session");
            Token = null;
        }
    }

    public async Task SignInAsync(AuthResponse auth)
    {
        (Token, Email, DisplayName) = (auth.Token, auth.Email, auth.DisplayName);
        Preferences.Default.Set("email", auth.Email);
        Preferences.Default.Set("name", auth.DisplayName);
        await SecureStorage.Default.SetAsync(TokenKey, auth.Token);
    }

    public void SignOut()
    {
        (Token, Email, DisplayName) = (null, "", "");
        SecureStorage.Default.Remove(TokenKey);
        Preferences.Default.Remove("email");
        Preferences.Default.Remove("name");
    }

    internal void Expire()
    {
        SignOut();
        Expired?.Invoke();
    }
}
