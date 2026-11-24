using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// Base for the screens of one open budget: knows the budget, the month, the caller's role, can switch month,
/// supports pull-to-refresh, and can confirm destructive actions. Tab pages are kept alive by the Shell, so every
/// budget-dependent value is re-announced each time the page appears (the open budget may have changed).
/// </summary>
public abstract partial class BudgetViewModel(AppState state, IDialogService dialogs, ILogger logger) : BaseViewModel(logger)
{
    protected AppState State { get; } = state;
    protected IDialogService Dialogs { get; } = dialogs;
    protected Guid BudgetId => State.BudgetId;

    [ObservableProperty] private string _monthText = "";
    [ObservableProperty] private bool _isRefreshing;

    public bool CanEdit => State.CanEdit;
    public bool IsReadOnly => !State.CanEdit;
    public bool IsOwner => State.IsOwner;
    public bool IsShared => !State.IsOwner;
    public string BudgetName => State.Budget?.Name ?? "";
    public string Currency => State.Budget?.Currency ?? "";
    public override bool ShowAddButton => CanEdit && !IsFormOpen;

    public override Task OnAppearingAsync()
    {
        OnBudgetChanged();
        return RunAsync(LoadAsync);
    }

    /// <summary>Loads the screen's data for the open budget and month.</summary>
    protected abstract Task LoadAsync(CancellationToken ct);

    [RelayCommand] private Task PreviousMonthAsync() => ShiftMonthAsync(-1);
    [RelayCommand] private Task NextMonthAsync() => ShiftMonthAsync(1);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunAsync(LoadAsync);
        IsRefreshing = false;
    }

    /// <summary>Asks first, then runs; used for destructive actions.</summary>
    protected async Task ConfirmThenRunAsync(string title, string message, string accept, Func<CancellationToken, Task> action)
    {
        if (await Dialogs.ConfirmAsync(title, message, accept)) await RunAsync(action);
    }

    protected virtual void OnBudgetChanged()
    {
        MonthText = State.MonthText;
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsOwner));
        OnPropertyChanged(nameof(IsShared));
        OnPropertyChanged(nameof(BudgetName));
        OnPropertyChanged(nameof(Currency));
        OnPropertyChanged(nameof(ShowAddButton));
    }

    private Task ShiftMonthAsync(int months)
    {
        if (IsBusy) return Task.CompletedTask;
        State.Month = State.Month.AddMonths(months);
        MonthText = State.MonthText;
        return RunAsync(LoadAsync);
    }
}
