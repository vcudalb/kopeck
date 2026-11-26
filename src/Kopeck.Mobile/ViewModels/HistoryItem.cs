namespace Kopeck.Mobile.ViewModels;

/// <summary>One step in an expense's history: "Edited", "by Bob · 2 Oct 2026, 18:40" and one line per changed value.</summary>
public sealed record HistoryItem(string Heading, string ByText, string Lines, Color Accent)
{
    public bool HasLines => Lines.Length > 0;
}
