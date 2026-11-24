namespace Kopeck.Mobile.Services;

/// <summary>The API server address: editable on the sign-in screen and stored on the device (not baked into the build).</summary>
public interface IServerSettings
{
    string BaseUrl { get; }

    /// <summary>Validates and stores the address. Release builds only accept https so credentials never travel in clear text.</summary>
    bool TrySetBaseUrl(string? input, out string error);
}

public sealed class ServerSettings(IPreferences preferences, IDeviceInfo device) : IServerSettings
{
    private const string UrlKey = "api_url";

    private string DefaultUrl => device.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";

    public string BaseUrl => preferences.Get(UrlKey, DefaultUrl);

    public bool TrySetBaseUrl(string? input, out string error)
    {
        error = "";
        if (!Uri.TryCreate(input?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            error = "Enter a full server address, e.g. https://budget.example.com";
            return false;
        }
#if !DEBUG
        if (uri.Scheme != Uri.UriSchemeHttps) { error = "The server address must use https."; return false; }
#endif
        preferences.Set(UrlKey, uri.GetLeftPart(UriPartial.Authority));
        return true;
    }
}
