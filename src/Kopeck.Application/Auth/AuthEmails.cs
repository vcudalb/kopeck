using Kopeck.Domain.Users;

namespace Kopeck.Application.Auth;

/// <summary>Email texts for the account flows, kept together so wording stays consistent.</summary>
internal static class AuthEmails
{
    private static readonly string Minutes = $"{User.CodeLifetime.TotalMinutes:0}";

    public static (string Subject, string Body) VerificationCode(string code) =>
        ("Confirm your Kopeck account", $"Your verification code is {code}. It expires in {Minutes} minutes.");

    public static (string Subject, string Body) ResetCode(string code) =>
        ("Reset your Kopeck password", $"Your password reset code is {code}. It expires in {Minutes} minutes. If you didn't ask for this, ignore this email.");

    public static (string Subject, string Body) AlreadyRegistered() =>
        ("Kopeck account", "Someone tried to register with this email, but an account already exists. If it was you, sign in or use 'Forgot password'.");
}
