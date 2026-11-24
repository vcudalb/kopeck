namespace Kopeck.Domain.Users;

/// <summary>
/// An account. Owns its email-verification, password-reset and lockout state so the rules live in one place
/// (codes are single-use, stored hashed, expire, and lock after too many wrong tries).
/// </summary>
public sealed class User
{
    public const int MaxCodeAttempts = 5;
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User() { }   // EF Core

    public static User Create(string normalizedEmail, DateTimeOffset now) => new() { Email = normalizedEmail, CreatedAt = now };

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = "";            // stored lower-cased
    public string DisplayName { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public bool EmailConfirmed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public string? VerifyCodeHash { get; private set; }
    public DateTimeOffset? VerifyCodeExpires { get; private set; }
    public int VerifyAttempts { get; private set; }

    public string? ResetCodeHash { get; private set; }
    public DateTimeOffset? ResetCodeExpires { get; private set; }
    public int ResetAttempts { get; private set; }

    public int FailedLogins { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    public void SetDisplayName(string displayName) => DisplayName = displayName.Trim();

    /// <summary>The hash is produced outside the domain (see the application's password hasher).</summary>
    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    /// <summary>Creates a fresh email-verification code and returns the plain value to email. Only its hash is stored.</summary>
    public string IssueVerifyCode(DateTimeOffset now)
    {
        var code = OneTimeCode.Generate();
        (VerifyCodeHash, VerifyCodeExpires, VerifyAttempts) = (OneTimeCode.Hash(code), now + CodeLifetime, 0);
        return code;
    }

    public bool TryConfirmEmail(string? code, DateTimeOffset now)
    {
        if (EmailConfirmed) return false;
        if (!OneTimeCode.IsValid(VerifyCodeHash, VerifyCodeExpires, VerifyAttempts, MaxCodeAttempts, code, now)) { VerifyAttempts++; return false; }
        EmailConfirmed = true;
        (VerifyCodeHash, VerifyCodeExpires, VerifyAttempts) = (null, null, 0);
        return true;
    }

    public string IssueResetCode(DateTimeOffset now)
    {
        var code = OneTimeCode.Generate();
        (ResetCodeHash, ResetCodeExpires, ResetAttempts) = (OneTimeCode.Hash(code), now + CodeLifetime, 0);
        return code;
    }

    /// <summary>Validates and consumes a reset code. The caller sets the new password hash when this returns true.</summary>
    public bool TryConsumeResetCode(string? code, DateTimeOffset now)
    {
        if (!OneTimeCode.IsValid(ResetCodeHash, ResetCodeExpires, ResetAttempts, MaxCodeAttempts, code, now)) { ResetAttempts++; return false; }
        (ResetCodeHash, ResetCodeExpires, ResetAttempts) = (null, null, 0);
        (FailedLogins, LockedUntil) = (0, null);
        return true;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil > now;

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        if (++FailedLogins < MaxFailedLogins) return;
        (FailedLogins, LockedUntil) = (0, now + LockoutDuration);
    }

    public void RegisterSuccessfulLogin() => (FailedLogins, LockedUntil) = (0, null);
}
