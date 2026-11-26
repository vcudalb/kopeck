using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Kopeck.Mobile.Views.Charts;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// The month at a glance: what is left (income − spent − saved) in the budget currency, plan vs real per category,
/// income per stream and money saved per pot. Amounts in other currencies are listed separately, never converted.
/// </summary>
public partial class DashboardViewModel(IBudgetsApi budgets, AppState state, INavigationService nav, IDialogService dialogs, ILogger<DashboardViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<CategoryRow> Rows { get; } = [];
    public ObservableCollection<AmountRow> Streams { get; } = [];
    public ObservableCollection<AmountRow> Savings { get; } = [];

    [ObservableProperty] private string _netText = "";
    [ObservableProperty] private string _netCaption = "";
    [ObservableProperty] private string _otherCurrenciesText = "";
    [ObservableProperty] private bool _hasOtherCurrencies;
    [ObservableProperty] private string _incomeText = "";
    [ObservableProperty] private string _plannedText = "";
    [ObservableProperty] private string _spentText = "";
    [ObservableProperty] private string _savedText = "";
    [ObservableProperty] private string _overText = "";
    [ObservableProperty] private bool _hasOverspend;
    [ObservableProperty] private bool _hasRows;
    [ObservableProperty] private bool _hasStreams;
    [ObservableProperty] private bool _hasSavings;
    [ObservableProperty] private string _sharedText = "";

    // "Where it went": this month's spending split by category
    public ObservableCollection<ShareRow> SpendRows { get; } = [];
    [ObservableProperty] private IChart? _spendChart;
    [ObservableProperty] private bool _hasSpending;

    [RelayCommand] private Task OpenPlanAsync() => nav.GoToAsync("plan");
    [RelayCommand] private Task OpenYearAsync() => nav.GoToAsync("year");
    [RelayCommand] private Task OpenSavingsAsync() => nav.GoToAsync("//main/savings");

    /// <summary>"+" on a category: opens the Expenses tab with a new expense in that category, dated in the month on screen.</summary>
    [RelayCommand]
    private Task AddExpenseAsync(CategoryRow row)
    {
        if (!CanEdit) return Task.CompletedTask;
        State.QuickAddCategoryId = row.CategoryId;
        return nav.GoToAsync("//main/expenses");
    }

    protected override async Task LoadAsync(CancellationToken ct)
    {
        var budget = State.Budget!;
        SharedText = budget.Role == BudgetRole.Owner ? "" : $"Shared by {budget.OwnerEmail} · {budget.Role}";

        var s = await budgets.GetSummaryAsync(BudgetId, State.Month, ct);
        var cur = budget.Currency;
        NetText = CurrencyText.Format(s.NetLeft, cur);
        NetCaption = s.NetLeft < 0 ? "More went out than came in this month" : "Left this month (income − spent − saved)";
        IncomeText = $"{s.TotalIncome:N2}";
        PlannedText = $"{s.TotalPlanned:N2}";
        SpentText = $"{s.TotalSpent:N2}";
        SavedText = $"{s.TotalSaved:N2}";
        HasOverspend = s.TotalOverspend > 0;
        OverText = $"Over plan by {CurrencyText.Format(s.TotalOverspend, cur)} across categories";

        // Income or savings in other currencies: shown under the main figure as "Also left: 300.00 USD".
        var others = s.Totals.Where(t => t.Currency != cur).ToList();
        HasOtherCurrencies = others.Count > 0;
        OtherCurrenciesText = HasOtherCurrencies ? "Also left: " + string.Join(" · ", others.Select(t => CurrencyText.Format(t.Left, t.Currency))) : "";

        Rows.ReplaceWith(s.Categories.Select(c => new CategoryRow(c, cur)));
        Streams.ReplaceWith(s.IncomeStreams.Select(i => new AmountRow(i.Name, CurrencyText.Format(i.Amount, i.Currency))));
        Savings.ReplaceWith(s.Savings.Select(p => new AmountRow(p.Name, (p.Amount < 0 ? "" : "+") + CurrencyText.Format(p.Amount, p.Currency))));
        HasRows = Rows.Count > 0;
        HasStreams = Streams.Count > 0;
        HasSavings = Savings.Count > 0;

        _summary = s;
        BuildSpendBreakdown(s.Categories, cur);
    }

    private MonthSummaryDto? _summary;

    public override Task OnAppearingAsync()
    {
        ChartTheme.Subscribe(this, () =>
        {
            if (_summary is { } s) BuildSpendBreakdown(s.Categories, s.Currency);
        });
        return base.OnAppearingAsync();
    }

    /// <summary>
    /// The split bar and its legend. Categories with their own colour get their own segment; the rest are merged into one
    /// neutral "other" segment, but still listed by name below the bar.
    /// </summary>
    private void BuildSpendBreakdown(IReadOnlyList<CategorySummaryDto> categories, string currency)
    {
        var colors = new CategoryColors(categories.Select(c => (c.CategoryId, c.Name)));
        var spent = categories.Where(c => c.Spent > 0).OrderByDescending(c => c.Spent).ToList();
        var total = spent.Sum(c => c.Spent);

        var segments = spent.Where(c => colors.HasOwnColor(c.CategoryId)).Select(c => new ShareSegment(c.Spent, colors.For(c.CategoryId))).ToList();
        var other = spent.Where(c => !colors.HasOwnColor(c.CategoryId)).Sum(c => c.Spent);
        if (other > 0) segments.Add(new ShareSegment(other, ChartPalette.Other));

        SpendChart = new ShareBarChart(segments);
        SpendRows.ReplaceWith(spent.Select(c => new ShareRow(
            c.Name, colors.For(c.CategoryId), CurrencyText.Format(c.Spent, currency), $"{c.Spent / total * 100:0}%")));
        HasSpending = spent.Count > 0;
    }
}
