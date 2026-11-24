using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>Status colours in one place, so every screen speaks the same visual language.</summary>
internal static class StatusPalette
{
    public static readonly Color Over = Color.FromArgb("#D32F2F");
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
    public Color StatusColor => StatusPalette.For(c.Status);
}

public partial class DashboardViewModel(
    IBudgetsApi budgets, AppState state, INavigationService nav, IDialogService dialogs, ILogger<DashboardViewModel> logger)
    : BudgetViewModel(state, dialogs, logger)
{
    public ObservableCollection<CategoryRow> Rows { get; } = [];
    public ObservableCollection<IncomeStreamSummaryDto> Streams { get; } = [];

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _incomeText = "";
    [ObservableProperty] private string _plannedText = "";
    [ObservableProperty] private string _spentText = "";
    [ObservableProperty] private string _netText = "";
    [ObservableProperty] private string _overText = "";
    [ObservableProperty] private bool _hasOverspend;
    [ObservableProperty] private Color _netColor = Colors.Gray;

    public override Task OnAppearingAsync() => RunAsync(LoadAsync);

    [RelayCommand] private Task PreviousMonthAsync() => ShiftMonthAsync(-1);
    [RelayCommand] private Task NextMonthAsync() => ShiftMonthAsync(1);
    [RelayCommand] private Task OpenExpensesAsync() => nav.GoToAsync("expenses");
    [RelayCommand] private Task OpenIncomeAsync() => nav.GoToAsync("income");
    [RelayCommand] private Task OpenPlanAsync() => nav.GoToAsync("plan");
    [RelayCommand] private Task OpenCategoriesAsync() => nav.GoToAsync("categories");
    [RelayCommand] private Task OpenShareAsync() => nav.GoToAsync("share");

    [RelayCommand]
    private Task LeaveAsync() => ConfirmThenRunAsync("Leave budget", "Remove this shared budget from your account?", "Leave", async ct =>
    {
        await budgets.LeaveAsync(BudgetId, ct);
        await nav.GoToAsync("//budgets");
    });

    [RelayCommand]
    private Task DeleteBudgetAsync() => ConfirmThenRunAsync("Delete budget",
        "Delete this budget with all its categories, plans, expenses and income? People it is shared with lose access. This can't be undone.",
        "Delete", async ct =>
        {
            await budgets.DeleteAsync(BudgetId, ct);
            State.Reset();
            await nav.GoToAsync("//budgets");
        });

    private async Task LoadAsync(CancellationToken ct)
    {
        var budget = State.Budget!;
        Title = budget.Role == BudgetRole.Owner ? budget.Name : $"{budget.Name} ({budget.Role}, by {budget.OwnerEmail})";
        RefreshMonthText();

        var s = await budgets.GetSummaryAsync(BudgetId, State.Month, ct);
        var cur = budget.Currency;
        IncomeText = $"{s.TotalIncome:N2} {cur}";
        PlannedText = $"{s.TotalPlanned:N2} {cur}";
        SpentText = $"{s.TotalSpent:N2} {cur}";
        NetText = $"{s.NetLeft:N2} {cur}";
        NetColor = s.NetLeft < 0 ? StatusPalette.Over : StatusPalette.Good;
        HasOverspend = s.TotalOverspend > 0;
        OverText = $"Overspent by {s.TotalOverspend:N2} {cur} across categories (vs plan)";

        Rows.ReplaceWith(s.Categories.Select(c => new CategoryRow(c, cur)));
        Streams.ReplaceWith(s.IncomeStreams);
    }

    private Task ShiftMonthAsync(int months)
    {
        State.Month = State.Month.AddMonths(months);
        return RunAsync(LoadAsync);
    }
}
