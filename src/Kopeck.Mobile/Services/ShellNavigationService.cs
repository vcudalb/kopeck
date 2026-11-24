namespace Kopeck.Mobile.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);
    public Task BackAsync() => Shell.Current.GoToAsync("..");
}
