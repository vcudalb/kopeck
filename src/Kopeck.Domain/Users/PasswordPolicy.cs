namespace Kopeck.Domain.Users;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>Returns the reason the password is rejected, or null when it is acceptable.</summary>
    public static string? Validate(string? password) =>
        string.IsNullOrEmpty(password) || password.Length < MinLength ? $"Password must be at least {MinLength} characters."
        : password.Length > MaxLength ? "Password is too long."
        : !password.Any(char.IsLetter) || !password.Any(char.IsDigit) ? "Password must contain letters and digits."
        : null;
}
