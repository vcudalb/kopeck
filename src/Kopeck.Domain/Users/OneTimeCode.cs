using System.Security.Cryptography;
using System.Text;

namespace Kopeck.Domain.Users;

/// <summary>6-digit one-time codes: generated with a CSPRNG, stored hashed, compared in constant time.</summary>
public static class OneTimeCode
{
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));

    public static bool IsValid(string? hash, DateTimeOffset? expires, int attempts, int maxAttempts, string? code, DateTimeOffset now) =>
        hash is not null && code is not null && expires > now && attempts < maxAttempts &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(Hash(code)));
}
