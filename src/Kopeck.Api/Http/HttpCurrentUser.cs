using System.Security.Claims;
using Kopeck.Application.Abstractions;
using Kopeck.Infrastructure.Identity;

namespace Kopeck.Api.Http;

/// <summary>Adapter: the signed-in caller as the application layer sees it, read from the validated JWT.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user
            ? user
            : throw new InvalidOperationException("No signed-in user: this code must only run on endpoints that require authorization.");

    public Guid Id => Guid.Parse(Principal.FindFirstValue(KopeckClaims.UserId)!);
    public string Email => Principal.FindFirstValue(KopeckClaims.Email)!.ToLowerInvariant();
    public string DisplayName => Principal.FindFirstValue(KopeckClaims.Name) ?? "";
}
