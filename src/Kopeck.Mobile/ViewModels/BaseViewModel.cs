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
