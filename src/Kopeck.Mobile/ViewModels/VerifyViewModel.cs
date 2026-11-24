using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kopeck.Contracts;
using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile.ViewModels;

public partial class VerifyViewModel(IAuthApi auth, IUserSession session, AppState state, INavigationService nav, ILogger<VerifyViewModel> logger)
    : BaseViewModel(logger)
{
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _info = "";

    public string Email => state.PendingEmail;

    [RelayCommand]
    private Task VerifyAsync() => RunAsync(async ct =>
    {
        await session.SignInAsync(await auth.VerifyEmailAsync(new EmailCodeRequest(Email, Code), ct));
        Code = "";
        await nav.GoToAsync("//budgets");
    });

    [RelayCommand]
    private Task ResendAsync() => RunAsync(async ct =>
    {
        await auth.ResendCodeAsync(Email, ct);
        Info = "A new code was sent.";
    });
}
