using System.Security.Cryptography;
using System.Text;
using Kopeck.Contracts;

namespace Kopeck.Api.Data;

public sealed class User
{
    public const int MaxCodeAttempts = 5;
    public const int MaxFailedLogins = 5;
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";            // stored lower-cased
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool EmailConfirmed { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string? VerifyCodeHash { get; private set; }
    public DateTimeOffset? VerifyCodeExpires { get; private set; }
    public int VerifyAttempts { get; private set; }

    public string? ResetCodeHash { get; private set; }
    public DateTimeOffset? ResetCodeExpires { get; private set; }
    public int ResetAttempts { get; private set; }

    public int FailedLogins { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

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
        FailedLogins = 0; LockedUntil = null;
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

/// <summary>6-digit one-time codes: generated with a CSPRNG, stored hashed, compared in constant time.</summary>
public static class OneTimeCode
{
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));

    public static bool IsValid(string? hash, DateTimeOffset? expires, int attempts, int maxAttempts, string? code, DateTimeOffset now) =>
        hash is not null && code is not null && expires > now && attempts < maxAttempts &&
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(Hash(code)));
}

public sealed class BudgetEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "EUR";
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
}

/// <summary>Shared by (verified) email, so it also works for people who register later.</summary>
public sealed class BudgetShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public string Email { get; set; } = "";            // lower-cased
    public ShareRole Role { get; set; }
}

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#6C757D";
}

public sealed class IncomeStream
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public string Name { get; set; } = "";
}

public sealed class Income
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public Guid StreamId { get; set; }
    public IncomeStream? Stream { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public sealed class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
}

/// <summary>Planned spend for one category in one month (Month is always the 1st).</summary>
public sealed class PlannedExpense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetId { get; set; }
    public Guid CategoryId { get; set; }
    public DateOnly Month { get; set; }
    public decimal Amount { get; set; }
}
