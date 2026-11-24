using CommunityToolkit.Mvvm.ComponentModel;
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

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(Error);

    /// <summary>Called by the page each time it appears (load data here).</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    /// <summary>Called by the page when it goes away: the request in flight, if any, is cancelled.</summary>
    public virtual void OnDisappearing() => _pending?.Cancel();

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

/// <summary>Base for the screens of one open budget: knows the budget, the month, the caller's role, and can confirm destructive actions.</summary>
public abstract partial class BudgetViewModel(AppState state, IDialogService dialogs, ILogger logger) : BaseViewModel(logger)
{
    protected AppState State { get; } = state;
    protected IDialogService Dialogs { get; } = dialogs;
    protected Guid BudgetId => State.BudgetId;

    [ObservableProperty] private string _monthText = "";

    public bool CanEdit => State.CanEdit;
    public bool IsOwner => State.IsOwner;

    /// <summary>Asks first, then runs; used for destructive actions.</summary>
    protected async Task ConfirmThenRunAsync(string title, string message, string accept, Func<CancellationToken, Task> action)
    {
        if (await Dialogs.ConfirmAsync(title, message, accept)) await RunAsync(action);
    }

    protected void RefreshMonthText() => MonthText = State.MonthText;
}
