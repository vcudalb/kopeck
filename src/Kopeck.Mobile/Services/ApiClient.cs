using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

/// <summary>
/// Wraps the app's single <see cref="HttpClient"/> (reusing it avoids socket exhaustion), adds the bearer token and turns
/// failures into <see cref="ApiException"/> with a message fit for the user. It knows nothing about screens:
/// a rejected token is reported through <see cref="IUserSession.Expired"/>.
/// </summary>
public sealed class ApiClient(HttpClient http, IServerSettings server, IUserSession session) : IApiClient
{
    public Task<T> GetAsync<T>(string path, CancellationToken ct) => SendAsync<T>(HttpMethod.Get, path, null, ct, expectBody: true);
    public Task<T> PostAsync<T>(string path, object? body, CancellationToken ct) => SendAsync<T>(HttpMethod.Post, path, body, ct, expectBody: true);
    public Task<T> PutAsync<T>(string path, object? body, CancellationToken ct) => SendAsync<T>(HttpMethod.Put, path, body, ct, expectBody: true);
    public Task PostAsync(string path, object? body, CancellationToken ct) => SendAsync<object>(HttpMethod.Post, path, body, ct, expectBody: false);
    public Task PutAsync(string path, object? body, CancellationToken ct) => SendAsync<object>(HttpMethod.Put, path, body, ct, expectBody: false);
    public Task DeleteAsync(string path, CancellationToken ct) => SendAsync<object>(HttpMethod.Delete, path, null, ct, expectBody: false);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct, bool expectBody)
    {
        using var request = new HttpRequestMessage(method, server.BaseUrl + path);
        if (session.Token is { } token) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json.Options);

        HttpResponseMessage response;
        try { response = await http.SendAsync(request, ct); }
        catch (HttpRequestException) { throw Unreachable(); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw Unreachable(); }   // timeout, not a cancel

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

    private ApiException Unreachable() =>
        new($"Can't reach the server at {server.BaseUrl}. Check your connection or the server address.", 0);
}
