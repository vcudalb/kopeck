using Kopeck.Api.Filters;
using Kopeck.Api.Http;
using Kopeck.Application.Auth;
using Kopeck.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kopeck.Api.Controllers;

/// <summary>
/// Registration, email verification, sign-in and password reset. Register, resend and forgot always answer 202,
/// so they can't be used to discover which emails are registered.
/// </summary>
[Route("api/auth")]
[EnableRateLimiting(RateLimitOptions.AuthPolicy)]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    [HttpPost("register")]
    [ServiceFilter(typeof(RequestValidationFilter<RegisterRequest>))]
    public async Task<IActionResult> Register(RegisterRequest request) =>
        FromResult(await auth.RegisterAsync(request), Accepted);

    [HttpPost("resend-code")]
    [ServiceFilter(typeof(RequestValidationFilter<EmailRequest>))]
    public async Task<IActionResult> ResendCode(EmailRequest request) =>
        FromResult(await auth.ResendCodeAsync(request), Accepted);

    [HttpPost("verify-email")]
    [ServiceFilter(typeof(RequestValidationFilter<EmailCodeRequest>))]
    public async Task<IActionResult> VerifyEmail(EmailCodeRequest request) =>
        FromResult(await auth.VerifyEmailAsync(request));

    [HttpPost("login")]
    [ServiceFilter(typeof(RequestValidationFilter<LoginRequest>))]
    public async Task<IActionResult> Login(LoginRequest request) =>
        FromResult(await auth.LoginAsync(request));

    [HttpPost("forgot-password")]
    [ServiceFilter(typeof(RequestValidationFilter<EmailRequest>))]
    public async Task<IActionResult> ForgotPassword(EmailRequest request) =>
        FromResult(await auth.ForgotPasswordAsync(request), Accepted);

    [HttpPost("reset-password")]
    [ServiceFilter(typeof(RequestValidationFilter<ResetPasswordRequest>))]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request) =>
        FromResult(await auth.ResetPasswordAsync(request));
}
