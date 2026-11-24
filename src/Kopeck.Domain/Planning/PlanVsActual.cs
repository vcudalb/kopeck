namespace Kopeck.Domain.Planning;

/// <summary>
/// The Excel "Left" column (<c>Planned - SUMIF(expenses in category)</c>) plus what the spreadsheet lacked:
/// <see cref="OverBy"/> says by how much a category went over its plan. A pure value, so it is trivially unit tested.
/// </summary>
public readonly record struct PlanVsActual(decimal Planned, decimal Spent)
{
    /// <summary>At or above this share of the plan a category is flagged as Warning.</summary>
    public const decimal WarningThreshold = 0.8m;

    /// <summary>Planned - Spent (negative when overspent).</summary>
    public decimal Left => Planned - Spent;

    /// <summary>How much more than planned was spent (0 if not over).</summary>
    public decimal OverBy => Math.Max(0, Spent - Planned);

    public decimal PercentUsed => Planned > 0 ? Math.Round(Spent / Planned * 100, 1) : Spent > 0 ? 100 : 0;

    public SpendingStatus Status =>
        Planned == 0 && Spent == 0 ? SpendingStatus.Unused
        : Planned == 0 ? SpendingStatus.Unplanned
        : Spent > Planned ? SpendingStatus.Over
        : Spent >= Planned * WarningThreshold ? SpendingStatus.Warning
        : SpendingStatus.OnTrack;
}
