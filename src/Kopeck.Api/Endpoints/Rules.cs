using Kopeck.Contracts;

namespace Kopeck.Api.Endpoints;

/// <summary>Input rules and error results shared by all endpoints.</summary>
public static class Rules
{
    public const decimal MaxAmount = 1_000_000_000_000m;

    public static bool IsValidName(string? n) => !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= 80;
    public static bool IsValidAmount(decimal a, bool allowZero = false) => (allowZero ? a >= 0 : a > 0) && a <= MaxAmount && decimal.Round(a, 2) == a;
    public static string? CleanNote(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim() is { Length: > 200 } t ? t[..200] : s.Trim();
    public static string? ValidColor(string? c) => c is { Length: 7 } && c[0] == '#' && c.Skip(1).All(Uri.IsHexDigit) ? c : null;
    public static DateOnly FirstOfMonth(DateOnly d) => new(d.Year, d.Month, 1);

    public static IResult Bad(string message) => Results.BadRequest(new ApiError(message));
    public static IResult Conflict(string message) => Results.Json(new ApiError(message), statusCode: StatusCodes.Status409Conflict);
}
