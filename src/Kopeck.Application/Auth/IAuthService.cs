using Kopeck.Application.Common;
using Kopeck.Contracts;

namespace Kopeck.Application.Auth;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request);
    Task<Result> ResendCodeAsync(EmailRequest request);
    Task<Result<AuthResponse>> VerifyEmailAsync(EmailCodeRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result> ForgotPasswordAsync(EmailRequest request);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
}
