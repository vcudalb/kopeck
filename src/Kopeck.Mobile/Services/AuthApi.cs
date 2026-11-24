using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

public sealed class AuthApi(IApiClient api) : IAuthApi
{
    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct) => api.PostAsync<AuthResponse>("/api/auth/login", request, ct);
    public Task RegisterAsync(RegisterRequest request, CancellationToken ct) => api.PostAsync("/api/auth/register", request, ct);
    public Task<AuthResponse> VerifyEmailAsync(EmailCodeRequest request, CancellationToken ct) => api.PostAsync<AuthResponse>("/api/auth/verify-email", request, ct);
    public Task ResendCodeAsync(string email, CancellationToken ct) => api.PostAsync("/api/auth/resend-code", new EmailRequest(email), ct);
    public Task ForgotPasswordAsync(string email, CancellationToken ct) => api.PostAsync("/api/auth/forgot-password", new EmailRequest(email), ct);
    public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct) => api.PostAsync("/api/auth/reset-password", request, ct);
}
