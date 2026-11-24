using System.Net.Mail;

namespace Kopeck.Domain.Common;

/// <summary>Input rules that belong to the business and are shared by every use case.</summary>
public static class DomainRules
{
    public const int MaxNameLength = 80;
    public const int MaxNoteLength = 200;
    public const int MaxEmailLength = 254;
    public const int MaxCurrencyLength = 5;
    public const int ColorLength = 7;
    public const decimal MaxAmount = 1_000_000_000_000m;
    public const string DefaultCategoryColor = "#6C757D";

    public static bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= MaxNameLength;

    /// <summary>Positive (or zero when allowed), not absurdly large, and at most 2 decimals.</summary>
    public static bool IsValidAmount(decimal amount, bool allowZero = false) =>
        (allowZero ? amount >= 0 : amount > 0) && amount <= MaxAmount && decimal.Round(amount, 2) == amount;

    public static string? CleanNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return null;
        var trimmed = note.Trim();
        return trimmed.Length > MaxNoteLength ? trimmed[..MaxNoteLength] : trimmed;
    }

    /// <summary>Returns the colour when it is <c>#RRGGBB</c>, otherwise null.</summary>
    public static string? ValidColor(string? color) =>
        color is { Length: ColorLength } && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit) ? color : null;

    /// <summary>Upper-cased 3-5 letter code (EUR, MDL, ...), or null when invalid.</summary>
    public static string? NormalizeCurrency(string? currency)
    {
        var code = (currency ?? "").Trim().ToUpperInvariant();
        return code.Length is >= 3 and <= MaxCurrencyLength && code.All(char.IsLetter) ? code : null;
    }

    /// <summary>Exactly 6 digits, the format of the codes sent by email.</summary>
    public static bool IsOneTimeCode(string? code) => code is { Length: 6 } && code.All(char.IsAsciiDigit);

    /// <summary>Lower-cased address, or null when it is not a plain valid email.</summary>
    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength) return null;
        var trimmed = email.Trim();
        try { return new MailAddress(trimmed).Address == trimmed ? trimmed.ToLowerInvariant() : null; }
        catch (FormatException) { return null; }
    }
}
