namespace Kopeck.Mobile.ViewModels;

/// <summary>One line of a chart legend: colour, name, amount and share of the total.</summary>
public sealed record ShareRow(string Name, Color Color, string AmountText, string PercentText);
