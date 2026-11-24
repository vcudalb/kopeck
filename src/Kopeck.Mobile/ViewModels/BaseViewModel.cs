using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// Busy/error state and page lifecycle shared by every screen. View models declare their own dependencies
/// (no service bag), so each constructor shows exactly what the screen uses.
/// </summary>
public abstract partial class BaseViewModel(ILogger logger) : ObservableObject
{
    private CancellationTokenSource? _pending;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsNotBusy))] private bool _isBusy;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _error = "";

    /// <summary>Whether the screen's add/edit sheet is showing.</summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowAddButton))] private bool _isFormOpen;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(Error);

    /// <summary>Whether the floating "+" button shows (hidden while the sheet is open).</summary>
    public virtual bool ShowAddButton => !IsFormOpen;

    /// <summary>Called by the page each time it appears (load data here).</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    /// <summary>Called by the page when it goes away: the request in flight, if any, is cancelled.</summary>
    public virtual void OnDisappearing() => _pending?.Cancel();

    [RelayCommand]
    private void OpenForm()
    {
        Error = "";
        OnFormOpening();
        IsFormOpen = true;
    }

    [RelayCommand]
    private void CloseForm()
    {
        IsFormOpen = false;
        Error = "";
        OnFormClosed();
    }

    /// <summary>Reset the form fields for a new entry.</summary>
    protected virtual void OnFormOpening() { }

    protected virtual void OnFormClosed() { }

    /// <summary>
    /// Runs a command body with a busy flag and a cancellation token tied to the page's lifetime, and turns failures
    /// into a visible message. Expected API errors show the server's text; anything else is logged and shown generically.
    /// </summary>
    protected async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        Error = "";
        using var cts = _pending = new CancellationTokenSource();
        try
        {
            await action(cts.Token);
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
        }
        catch (OperationCanceledException)
        {
            // the page went away; nothing to show
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in {ViewModel}", GetType().Name);
            Error = "Something went wrong. Please try again.";
        }
        finally
        {
            _pending = null;
            IsBusy = false;
        }
    }
}

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
