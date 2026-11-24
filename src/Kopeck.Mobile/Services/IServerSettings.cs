namespace Kopeck.Mobile.Services;

/// <summary>The API server address: editable on the sign-in screen and stored on the device (not baked into the build).</summary>
public interface IServerSettings
{
    string BaseUrl { get; }

    /// <summary>Validates and stores the address. Release builds only accept https so credentials never travel in clear text.</summary>
    bool TrySetBaseUrl(string? input, out string error);
}
