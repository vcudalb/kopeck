using Kopeck.Contracts;

namespace Kopeck.Mobile.Services;

// Typed gateways, one per feature (interface segregation): a view model depends only on the calls it makes,
// and every URL of the API is written in exactly one place.

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> VerifyEmailAsync(EmailCodeRequest request, CancellationToken ct);
    Task ResendCodeAsync(string email, CancellationToken ct);
    Task ForgotPasswordAsync(string email, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
}
