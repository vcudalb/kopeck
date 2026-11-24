using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kopeck.Application.Abstractions;
using Kopeck.Domain.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kopeck.Infrastructure.Identity;

/// <summary>Signs and validates JWTs. The key comes from configuration and is never hard-coded.</summary>
public sealed class JwtTokenService : ITokenService
{
    private const string Issuer = "kopeck-api";
    private const int MinKeyBytes = 32;
    private readonly SymmetricSecurityKey _key;
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IHostEnvironment env, TimeProvider clock)
    {
        _clock = clock;
        _lifetime = TimeSpan.FromHours(options.Value.LifetimeHours);

        var raw = options.Value.Key;
        if (string.IsNullOrWhiteSpace(raw))
        {
            if (!env.IsDevelopment())
                throw new InvalidOperationException("Jwt:Key is not configured (set env var Jwt__Key to a random 32+ byte secret).");
            raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)); // ephemeral dev key: tokens die on restart
        }
        if (Encoding.UTF8.GetByteCount(raw) < MinKeyBytes) throw new InvalidOperationException($"Jwt:Key must be at least {MinKeyBytes} bytes.");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(raw));
    }

    public TokenValidationParameters ValidationParameters => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = _key,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = KopeckClaims.Name,
    };

    public AccessToken Create(User user)
    {
        var now = _clock.GetUtcNow();
        var expires = now + _lifetime;
        var jwt = new JwtSecurityToken(Issuer, audience: null,
            [new Claim(KopeckClaims.UserId, user.Id.ToString()), new Claim(KopeckClaims.Email, user.Email), new Claim(KopeckClaims.Name, user.DisplayName)],
            now.UtcDateTime, expires.UtcDateTime, new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
