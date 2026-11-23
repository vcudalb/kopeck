namespace Kopeck.Mobile.Services;

/// <summary>Navigation abstraction so view models never touch <c>Shell.Current</c> (keeps them unit-testable).</summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task BackAsync();
}

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    Task<string?> PromptAsync(string title, string message, string? initialValue = null, int maxLength = -1);
    Task AlertAsync(string title, string message);
}

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);
    public Task BackAsync() => Shell.Current.GoToAsync("..");
}

public sealed class ShellDialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel") =>
        Shell.Current.DisplayAlert(title, message, accept, cancel);

    public Task<string?> PromptAsync(string title, string message, string? initialValue = null, int maxLength = -1) =>
        Shell.Current.DisplayPromptAsync(title, message, initialValue: initialValue ?? "", maxLength: maxLength);

    public Task AlertAsync(string title, string message) => Shell.Current.DisplayAlert(title, message, "OK");
}
