using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Kopeck.Mobile.Views.Charts;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// The year at a glance: totals, income vs spent per month (or one category's spent vs planned, picked from the filter
/// or by tapping it in the ranking), and every category's share of the year. Tapping a month shows its numbers as text.
/// Amounts are in the budget currency only.
/// </summary>
public partial class YearViewModel(IBudgetsApi budgets, AppState state, IDialogService dialogs, ILogger<YearViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    private const string AllCategories = "All categories";

    private YearSummaryDto? _data;
    private CategoryColors? _colors;
    private int _year = state.Month.Year;

    public ObservableCollection<LegendItem> Legend { get; } = [];
    public ObservableCollection<CategoryYearRow> Categories { get; } = [];
    public ObservableCollection<string> FilterOptions { get; } = [AllCategories];

    [ObservableProperty] private string _yearText = "";
    [ObservableProperty] private string _incomeText = "";
    [ObservableProperty] private string _spentText = "";
    [ObservableProperty] private string _savedText = "";
    [ObservableProperty] private string _leftText = "";
    [ObservableProperty] private string _currencyNote = "";
    [ObservableProperty] private string _chartTitle = "";
    [ObservableProperty] private IChart? _monthlyChart;
    [ObservableProperty] private int? _selectedMonth;
    [ObservableProperty] private string _selectedMonthText = "";
    [ObservableProperty] private bool _hasCategories;
    [ObservableProperty] private string _filter = AllCategories;

    partial void OnFilterChanged(string value) => BuildChart();
    partial void OnSelectedMonthChanged(int? value) => BuildChart();

    [RelayCommand] private Task PreviousYearAsync() => ShiftYearAsync(-1);
    [RelayCommand] private Task NextYearAsync() => ShiftYearAsync(1);

    [RelayCommand]
    private void SelectMonth(int month) => SelectedMonth = SelectedMonth == month ? null : month;

    [RelayCommand]
    private void ShowCategory(CategoryYearRow row) => Filter = Filter == row.Name ? AllCategories : row.Name;

    protected override async Task LoadAsync(CancellationToken ct)
    {
        YearText = _year.ToString(CultureInfo.InvariantCulture);
        var data = await budgets.GetYearSummaryAsync(BudgetId, _year, ct);
        _data = data;
        _colors = new CategoryColors(data.Categories.Select(c => (c.CategoryId, c.Name)));

        var cur = data.Currency;
        var income = data.Months.Sum(m => m.Income);
        var spent = data.Months.Sum(m => m.Spent);
        var saved = data.Months.Sum(m => m.Saved);
        IncomeText = $"{income:N2}";
        SpentText = $"{spent:N2}";
        SavedText = $"{saved:N2}";
        LeftText = CurrencyText.Format(income - spent - saved, cur);
        CurrencyNote = $"All amounts in {cur}. Income and savings in other currencies are not included.";

        // keep the chosen category if it still exists this year
        var names = data.Categories.Select(c => c.Name).ToList();
        if (Filter != AllCategories && !names.Contains(Filter)) Filter = AllCategories;
        var keep = Filter;
        FilterOptions.ReplaceWith([AllCategories, .. names]);
        Filter = keep;

        BuildCategoryRows();

        // open on the month being looked at elsewhere in the app, when it is in this year
        SelectedMonth = State.Month.Year == _year ? State.Month.Month - 1 : null;
        BuildChart();
    }

    public override Task OnAppearingAsync()
    {
        ChartTheme.Subscribe(this, () => { BuildCategoryRows(); BuildChart(); });
        return base.OnAppearingAsync();
    }

    private void BuildCategoryRows()
    {
        if (_data is null || _colors is null) return;
        var spent = _data.Categories.Sum(c => c.Spent);
        var max = _data.Categories.Select(c => c.Spent).DefaultIfEmpty(0).Max();
        Categories.ReplaceWith(_data.Categories.Where(c => c.Spent > 0).OrderByDescending(c => c.Spent).Select(c =>
        {
            var color = _colors.For(c.CategoryId);
            return new CategoryYearRow(c.CategoryId, c.Name, color, CurrencyText.Format(c.Spent, _data.Currency),
                $"{c.Spent / spent * 100:0}%", new RowBarChart((double)(c.Spent / max), color));
        }));
        HasCategories = Categories.Count > 0;
    }

    private void BuildChart()
    {
        if (_data is null || _colors is null) return;
        var cur = _data.Currency;
        var category = _data.Categories.FirstOrDefault(c => c.Name == Filter);
        string MonthName(int m) => new DateTime(_year, m + 1, 1).ToString("MMMM", CultureInfo.CurrentCulture);

        if (category is null)
        {
            var income = new BarSeries("Income", _data.Months.Select(m => m.Income).ToList(), ChartPalette.Series1);
            var spent = new BarSeries("Spent", _data.Months.Select(m => m.Spent).ToList(), ChartPalette.Series2);
            ChartTitle = "Income vs spent";
            MonthlyChart = new MonthlyBarsChart([income, spent], null, SelectedMonth);
            Legend.ReplaceWith([new LegendItem(income.Name, income.Color, false), new LegendItem(spent.Name, spent.Color, false)]);
            SelectedMonthText = SelectedMonth is { } m
                ? $"{MonthName(m)}: income {CurrencyText.Format(_data.Months[m].Income, cur)} · spent {CurrencyText.Format(_data.Months[m].Spent, cur)} · saved {CurrencyText.Format(_data.Months[m].Saved, cur)}"
                : "Tap a month to see its numbers.";
        }
        else
        {
            var color = _colors.For(category.CategoryId);
            var spent = new BarSeries("Spent", category.MonthlySpent, color);
            ChartTitle = $"{category.Name}: spent vs planned";
            MonthlyChart = new MonthlyBarsChart([spent], category.MonthlyPlanned, SelectedMonth);
            Legend.ReplaceWith([new LegendItem("Spent", color, false), new LegendItem("Planned", ChartPalette.Marker, true)]);
            SelectedMonthText = SelectedMonth is { } m
                ? $"{MonthName(m)}: spent {CurrencyText.Format(category.MonthlySpent[m], cur)} of {CurrencyText.Format(category.MonthlyPlanned[m], cur)} planned"
                : "Tap a month to see its numbers.";
        }
    }

    private Task ShiftYearAsync(int years)
    {
        if (IsBusy) return Task.CompletedTask;
        _year += years;
        return RunAsync(LoadAsync);
    }
}
