using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Status colours in one place, so every screen speaks the same visual language.</summary>
internal static class StatusPalette
{
    public static readonly Color Over = Color.FromArgb("#C62828");
    public static readonly Color Warning = Color.FromArgb("#F9A825");
    public static readonly Color Unplanned = Color.FromArgb("#6A1B9A");
    public static readonly Color Unused = Color.FromArgb("#9E9E9E");
    public static readonly Color Good = Color.FromArgb("#2E7D32");

    public static Color For(CategoryStatus status) => status switch
    {
        CategoryStatus.Over => Over,
        CategoryStatus.Warning => Warning,
        CategoryStatus.Unplanned => Unplanned,
        CategoryStatus.Unused => Unused,
        _ => Good,
    };
}

/// <summary>Presentation of one category's planned-vs-real numbers.</summary>
public sealed class CategoryRow(CategorySummaryDto c, string currency)
{
    public string Name => c.Name;

    /// <summary>"120.00 of 300.00 EUR" (or just the spent amount when there is no plan).</summary>
    public string SpentOfPlanText => c.Planned > 0 ? $"{c.Spent:N2} of {c.Planned:N2} {currency}" : $"{c.Spent:N2} {currency} spent";

    public string PercentText => c.Status == CategoryStatus.Unused ? "" : $"{c.PercentUsed:0}%";

    /// <summary>The key line: money left, or by how much the category is over its plan.</summary>
    public string LeftText => c.Status switch
    {
        CategoryStatus.Over => $"Over by {c.OverBy:N2}",
        CategoryStatus.Unplanned => "Not planned",
        CategoryStatus.Unused => "No plan, no spending",
        _ => $"{c.Left:N2} left",
    };

    public double Progress => Math.Min(1.0, (double)c.PercentUsed / 100.0);
    public Color StatusColor => StatusPalette.For(c.Status);
}

/// <summary>A labelled amount, e.g. one income stream.</summary>
public sealed record AmountRow(string Name, string AmountText);

public partial class DashboardViewModel(IBudgetsApi budgets, AppState state, IDialogService dialogs, ILogger<DashboardViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<CategoryRow> Rows { get; } = [];
    public ObservableCollection<AmountRow> Streams { get; } = [];

    [ObservableProperty] private string _netText = "";
    [ObservableProperty] private string _netCaption = "";
    [ObservableProperty] private string _incomeText = "";
    [ObservableProperty] private string _plannedText = "";
    [ObservableProperty] private string _spentText = "";
    [ObservableProperty] private string _overText = "";
    [ObservableProperty] private bool _hasOverspend;
    [ObservableProperty] private bool _hasRows;
    [ObservableProperty] private bool _hasStreams;
    [ObservableProperty] private string _sharedText = "";

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var budget = State.Budget!;
        SharedText = budget.Role == BudgetRole.Owner ? "" : $"Shared by {budget.OwnerEmail} · {budget.Role}";

        var s = await budgets.GetSummaryAsync(BudgetId, State.Month, ct);
        var cur = budget.Currency;
        NetText = $"{s.NetLeft:N2} {cur}";
        NetCaption = s.NetLeft < 0 ? "Spent more than earned this month" : "Left this month (income − spent)";
        IncomeText = $"{s.TotalIncome:N2}";
        PlannedText = $"{s.TotalPlanned:N2}";
        SpentText = $"{s.TotalSpent:N2}";
        HasOverspend = s.TotalOverspend > 0;
        OverText = $"Over plan by {s.TotalOverspend:N2} {cur} across categories";

        Rows.ReplaceWith(s.Categories.Select(c => new CategoryRow(c, cur)));
        Streams.ReplaceWith(s.IncomeStreams.Select(i => new AmountRow(i.Name, $"{i.Amount:N2} {cur}")));
        HasRows = Rows.Count > 0;
        HasStreams = Streams.Count > 0;
    }
}
