namespace Kopeck.Mobile.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    Task<string?> PromptAsync(string title, string message, string? initialValue = null, int maxLength = -1);
    Task AlertAsync(string title, string message);

    /// <summary>Lets the user pick one option; null when cancelled.</summary>
    Task<string?> ChooseAsync(string title, IReadOnlyList<string> options);
}
