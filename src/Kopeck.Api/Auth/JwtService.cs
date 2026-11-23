using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kopeck.Api.Data;
using Microsoft.IdentityModel.Tokens;

namespace Kopeck.Api.Auth;

/// <summary>Signs and validates JWTs. The key comes from configuration (Jwt:Key) and is never hard-coded.</summary>
public sealed class JwtService
{
    private const string Issuer = "kopeck-api";
    private readonly SymmetricSecurityKey _key;
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _clock;

    public JwtService(IConfiguration cfg, IHostEnvironment env, TimeProvider clock)
    {
        _clock = clock;
        _lifetime = TimeSpan.FromHours(cfg.GetValue("Jwt:LifetimeHours", 24 * 7));

        var raw = cfg["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (!env.IsDevelopment())
                throw new InvalidOperationException("Jwt:Key is not configured (set env var Jwt__Key to a random 32+ byte secret).");
            raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)); // ephemeral dev key: tokens die on restart
        }
        if (Encoding.UTF8.GetByteCount(raw) < 32) throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(raw));
    }

    public TokenValidationParameters ValidationParameters => new()
    {
        ValidateIssuer = true, ValidIssuer = Issuer,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true, IssuerSigningKey = _key,
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = "name",
    };

    public (string Token, DateTimeOffset Expires) Create(User u)
    {
        var now = _clock.GetUtcNow();
        var expires = now + _lifetime;
        var jwt = new JwtSecurityToken(Issuer, null,
            [new Claim("sub", u.Id.ToString()), new Claim("email", u.Email), new Claim("name", u.DisplayName)],
            now.UtcDateTime, expires.UtcDateTime, new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue("sub")!);
    public static string UserEmail(this ClaimsPrincipal p) => p.FindFirstValue("email")!.ToLowerInvariant();
    public static string UserName(this ClaimsPrincipal p) => p.FindFirstValue("name") ?? "";
}
