using Kopeck.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace Kopeck.Api.Auth;

public static class PasswordPolicy
{
    public static string? Validate(string? p) =>
        string.IsNullOrEmpty(p) || p.Length < 8 ? "Password must be at least 8 characters."
        : p.Length > 128 ? "Password is too long."
        : !p.Any(char.IsLetter) || !p.Any(char.IsDigit) ? "Password must contain letters and digits."
        : null;
}

/// <summary>PBKDF2 via ASP.NET Identity's hasher (salted, versioned, upgrades iteration counts over time).</summary>
public static class PasswordService
{
    private static readonly PasswordHasher<User> Hasher = new();
    // Verified against when the account doesn't exist, so response time doesn't reveal which emails are registered.
    private static readonly string DummyHash = Hasher.HashPassword(new User(), Guid.NewGuid().ToString("N"));

    public static string Hash(User u, string password) => Hasher.HashPassword(u, password);

    public static bool Verify(User? u, string password)
    {
        if (u is null) { Hasher.VerifyHashedPassword(new User(), DummyHash, password); return false; }
        return Hasher.VerifyHashedPassword(u, u.PasswordHash, password) != PasswordVerificationResult.Failed;
    }
}
