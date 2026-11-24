using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

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
