using Kopeck.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

/// <summary>The services every view model needs, bundled so constructors stay short.</summary>
public sealed class ViewModelContext(ApiClient api, AppState state, INavigationService nav, IDialogService dialogs, ILoggerFactory logs)
{
    public ApiClient Api { get; } = api;
    public AppState State { get; } = state;
    public INavigationService Nav { get; } = nav;
    public IDialogService Dialogs { get; } = dialogs;
    public ILoggerFactory Logs { get; } = logs;
}

public abstract partial class BaseViewModel(ViewModelContext context) : ObservableObject
{
    private readonly ILogger _log = context.Logs.CreateLogger(typeof(BaseViewModel));

    protected ApiClient Api => context.Api;
    protected INavigationService Nav => context.Nav;
    protected IDialogService Dialogs => context.Dialogs;
    public AppState State => context.State;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsNotBusy))] private bool _isBusy;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _error = "";

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool CanEdit => State.CanEdit;

    /// <summary>Base path of the open budget's API, e.g. <c>/api/budgets/{id}</c>.</summary>
    protected string BudgetPath => $"/api/budgets/{State.Budget?.Id}";

    /// <summary>
    /// Runs a command body with a busy flag and turns failures into a visible message.
    /// Expected API errors show the server's text; anything else is logged and shown generically.
    /// </summary>
    protected async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true; Error = "";
        try { await action(); }
        catch (ApiException ex) { Error = ex.Message; }
        catch (OperationCanceledException) { /* navigated away */ }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled error in {ViewModel}", GetType().Name);
            Error = "Something went wrong. Please try again.";
        }
        finally { IsBusy = false; }
    }

    /// <summary>Asks first, then runs; used for destructive actions.</summary>
    protected async Task ConfirmThenRunAsync(string title, string message, string accept, Func<Task> action)
    {
        if (await Dialogs.ConfirmAsync(title, message, accept)) await RunAsync(action);
    }
}
