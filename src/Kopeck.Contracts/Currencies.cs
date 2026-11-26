namespace Kopeck.Contracts;

/// <summary>Currencies offered in the app's pickers. The API accepts any 3-5 letter code, so this list can grow freely.</summary>
public static class Currencies
{
    public const string Default = "EUR";

    public static IReadOnlyList<string> Common { get; } =
    [
        "EUR", "USD", "GBP", "CHF", "MDL", "RON", "UAH", "PLN", "CZK", "HUF", "BGN", "SEK", "NOK", "DKK", "TRY", "CAD", "AUD", "JPY",
    ];
}
