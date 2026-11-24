using Kopeck.Api.Http;
using Kopeck.Application.Auth;
using Kopeck.Contracts;

namespace Kopeck.Api.Endpoints;

public sealed class AuthEndpoints : IEndpointModule
{
    public void Map(IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").RequireRateLimiting(RateLimitOptions.AuthPolicy);

        // Register, resend and forgot always answer 202 so they can't be used to discover which emails are registered.
        auth.MapPost("/register", async (RegisterRequest r, IAuthService svc) =>
            (await svc.RegisterAsync(r)).ToHttp(() => Results.Accepted()));

        auth.MapPost("/resend-code", async (EmailRequest r, IAuthService svc) =>
            (await svc.ResendCodeAsync(r)).ToHttp(() => Results.Accepted()));

        auth.MapPost("/verify-email", async (EmailCodeRequest r, IAuthService svc) =>
            (await svc.VerifyEmailAsync(r)).ToHttp());

        auth.MapPost("/login", async (LoginRequest r, IAuthService svc) =>
            (await svc.LoginAsync(r)).ToHttp());

        auth.MapPost("/forgot-password", async (EmailRequest r, IAuthService svc) =>
            (await svc.ForgotPasswordAsync(r)).ToHttp(() => Results.Accepted()));

        auth.MapPost("/reset-password", async (ResetPasswordRequest r, IAuthService svc) =>
            (await svc.ResetPasswordAsync(r)).ToHttp());
    }
}
