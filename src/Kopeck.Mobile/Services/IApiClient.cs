namespace Kopeck.Mobile.Services;

/// <summary>Raw HTTP transport. View models never use it directly: they go through the typed feature APIs in <c>KopeckApi.cs</c>.</summary>
public interface IApiClient
{
    Task<T> GetAsync<T>(string path, CancellationToken ct);
    Task<T> PostAsync<T>(string path, object? body, CancellationToken ct);
    Task<T> PutAsync<T>(string path, object? body, CancellationToken ct);
    Task PostAsync(string path, object? body, CancellationToken ct);
    Task PutAsync(string path, object? body, CancellationToken ct);
    Task DeleteAsync(string path, CancellationToken ct);
}
