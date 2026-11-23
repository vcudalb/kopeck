using System.Collections.ObjectModel;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Presentation of one category's planned-vs-real numbers.</summary>
public sealed class CategoryRow(CategorySummaryDto c, string currency)
{
    public string Name => c.Name;
    public string PlannedText => $"Planned {c.Planned:N2}";
    public string SpentText => $"Spent {c.Spent:N2}";

    /// <summary>The key line: money left, or by how much the category is over its plan.</summary>
    public string LeftText => c.Status switch
    {
        CategoryStatus.Over => $"Over by {c.OverBy:N2} {currency}",
        CategoryStatus.Unplanned => $"Unplanned: {c.OverBy:N2} {currency}",
        CategoryStatus.Unused => "No plan, no spending",
        _ => $"Left {c.Left:N2} {currency}",
    };

    public double Progress => Math.Min(1.0, (double)c.PercentUsed / 100.0);

    public Color StatusColor => c.Status switch
    {
        CategoryStatus.Over => Color.FromArgb("#D32F2F"),
        CategoryStatus.Warning => Color.FromArgb("#F9A825"),
        CategoryStatus.Unplanned => Color.FromArgb("#6A1B9A"),
        CategoryStatus.Unused => Color.FromArgb("#9E9E9E"),
        _ => Color.FromArgb("#2E7D32"),
    };
}

public partial class DashboardViewModel(ViewModelContext context) : BaseViewModel(context)
{
    public ObservableCollection<CategoryRow> Rows { get; } = [];
    public ObservableCollection<IncomeStreamSummaryDto> Streams { get; } = [];

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _monthText = "";
    [ObservableProperty] private string _incomeText = "";
    [ObservableProperty] private string _plannedText = "";
    [ObservableProperty] private string _spentText = "";
    [ObservableProperty] private string _netText = "";
    [ObservableProperty] private string _overText = "";
    [ObservableProperty] private bool _hasOverspend;
    [ObservableProperty] private Color _netColor = Colors.Gray;

    public bool IsOwner => State.IsOwner;

    [RelayCommand]
    private Task LoadAsync() => RunAsync(async () =>
    {
        var budget = State.Budget!;
        Title = budget.Role == BudgetRole.Owner ? budget.Name : $"{budget.Name} ({budget.Role}, by {budget.OwnerEmail})";
        MonthText = State.Month.ToString("MMMM yyyy");

        var s = await Api.GetAsync<MonthSummaryDto>($"{BudgetPath}/summary?month={ApiClient.Month(State.Month)}");
        var cur = budget.Currency;
        IncomeText = $"{s.TotalIncome:N2} {cur}";
        PlannedText = $"{s.TotalPlanned:N2} {cur}";
        SpentText = $"{s.TotalSpent:N2} {cur}";
        NetText = $"{s.NetLeft:N2} {cur}";
        NetColor = Color.FromArgb(s.NetLeft < 0 ? "#D32F2F" : "#2E7D32");
        HasOverspend = s.TotalOverspend > 0;
        OverText = $"Overspent by {s.TotalOverspend:N2} {cur} across categories (vs plan)";

        Rows.Clear();
        foreach (var c in s.Categories) Rows.Add(new CategoryRow(c, cur));
        Streams.Clear();
        foreach (var i in s.IncomeStreams) Streams.Add(i);
    });

    [RelayCommand] private Task PreviousMonthAsync() => ShiftMonthAsync(-1);
    [RelayCommand] private Task NextMonthAsync() => ShiftMonthAsync(1);
    [RelayCommand] private Task OpenExpensesAsync() => Nav.GoToAsync("expenses");
    [RelayCommand] private Task OpenIncomeAsync() => Nav.GoToAsync("income");
    [RelayCommand] private Task OpenPlanAsync() => Nav.GoToAsync("plan");
    [RelayCommand] private Task OpenCategoriesAsync() => Nav.GoToAsync("categories");
    [RelayCommand] private Task OpenShareAsync() => Nav.GoToAsync("share");

    [RelayCommand]
    private Task LeaveAsync() => ConfirmThenRunAsync("Leave budget", "Remove this shared budget from your account?", "Leave", async () =>
    {
        await Api.DeleteAsync($"{BudgetPath}/leave");
        await Nav.GoToAsync("//budgets");
    });

    private Task ShiftMonthAsync(int months)
    {
        State.Month = State.Month.AddMonths(months);
        return LoadAsync();
    }
}
