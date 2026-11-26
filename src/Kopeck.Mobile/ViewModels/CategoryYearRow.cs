using Kopeck.Mobile.Views.Charts;

namespace Kopeck.Mobile.ViewModels;

/// <summary>One category in the year's ranking: colour, name, total, share of the year, and a bar relative to the largest.</summary>
public sealed record CategoryYearRow(Guid CategoryId, string Name, Color Color, string AmountText, string PercentText, IChart Bar);
