namespace Kopeck.Mobile.Services;

public sealed class ShellDialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel") =>
        Shell.Current.DisplayAlert(title, message, accept, cancel);

    public Task<string?> PromptAsync(string title, string message, string? initialValue = null, int maxLength = -1) =>
        Shell.Current.DisplayPromptAsync(title, message, initialValue: initialValue ?? "", maxLength: maxLength);

    public Task AlertAsync(string title, string message) => Shell.Current.DisplayAlert(title, message, "OK");
}
