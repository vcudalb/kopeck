using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class ApiException(string message, HttpStatusCode status) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>
/// Thin typed wrapper over one shared <see cref="HttpClient"/> (the app's singleton; reusing it avoids socket exhaustion).
/// It knows nothing about screens: a rejected token is reported through <see cref="Session.Expired"/>.
/// </summary>
public sealed class ApiClient(Session session)
{
    private const string UrlKey = "api_url";
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    private static string DefaultUrl =>
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080";

    /// <summary>Server address, editable on the sign-in screen and stored on the device (not baked into the build).</summary>
    public static string BaseUrl => Preferences.Default.Get(UrlKey, DefaultUrl);

    /// <summary>Validates and stores the server address. Release builds only accept https so credentials never travel in clear text.</summary>
    public static bool TrySetBaseUrl(string? input, out string error)
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
        Preferences.Default.Set(UrlKey, uri.GetLeftPart(UriPartial.Authority));
        return true;
    }

    public static string Month(DateOnly m) => m.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    public Task<T> GetAsync<T>(string path, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Get, path, null, ct);
    public Task<T> PostAsync<T>(string path, object? body = null, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Post, path, body, ct);
    public Task<T> PutAsync<T>(string path, object? body = null, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Put, path, body, ct);
    public Task PostAsync(string path, object? body = null, CancellationToken ct = default) => SendAsync<object>(HttpMethod.Post, path, body, ct, expectBody: false);
    public Task PutAsync(string path, object? body = null, CancellationToken ct = default) => SendAsync<object>(HttpMethod.Put, path, body, ct, expectBody: false);
    public Task DeleteAsync(string path, CancellationToken ct = default) => SendAsync<object>(HttpMethod.Delete, path, null, ct, expectBody: false);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct, bool expectBody = true)
    {
        using var request = new HttpRequestMessage(method, BaseUrl + path);
        if (session.Token is { } token) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json.Options);

        HttpResponseMessage response;
        try { response = await _http.SendAsync(request, ct); }
        catch (HttpRequestException) { throw Unreachable(); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw Unreachable(); }   // timeout, not user cancel

        using (response)
        {
            if (response.IsSuccessStatusCode)
                return expectBody ? (await response.Content.ReadFromJsonAsync<T>(Json.Options, ct))! : default!;

            if (response.StatusCode == HttpStatusCode.Unauthorized && session.IsSignedIn)
            {
                session.Expire();
                throw new ApiException("Your session expired. Please sign in again.", response.StatusCode);
            }
            throw new ApiException(await ErrorMessageAsync(response, ct), response.StatusCode);
        }
    }

    private static async Task<string> ErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests) return "Too many attempts. Wait a minute and try again.";
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(Json.Options, ct);
            if (!string.IsNullOrWhiteSpace(error?.Error)) return error.Error;
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException) { /* not our error shape */ }
        return $"Request failed ({(int)response.StatusCode}).";
    }

    private static ApiException Unreachable() =>
        new($"Can't reach the server at {BaseUrl}. Check your connection or the server address.", 0);
}
