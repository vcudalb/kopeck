using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class ResetViewModel(IAuthApi auth, IDialogService dialogs, INavigationService nav, ILogger<ResetViewModel> logger)
    : BaseViewModel(logger)
{
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _info = "";

    [RelayCommand]
    private Task SendCodeAsync() => RunAsync(async ct =>
    {
        await auth.ForgotPasswordAsync(Email, ct);
        Info = "If that email has an account, a code was sent.";
    });

    [RelayCommand]
    private Task ResetAsync() => RunAsync(async ct =>
    {
        await auth.ResetPasswordAsync(new ResetPasswordRequest(Email, Code, NewPassword), ct);
        (NewPassword, Code) = ("", "");
        await dialogs.AlertAsync("Password changed", "You can now sign in.");
        await nav.BackAsync();
    });
}
