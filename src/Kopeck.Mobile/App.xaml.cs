using Kopeck.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Kopeck.Mobile;

public partial class App : Application
{
    private readonly Session _session;
    private readonly INavigationService _nav;
    private readonly ILogger<App> _log;

    public App(Session session, INavigationService nav, ILogger<App> log)
    {
        InitializeComponent();
        (_session, _nav, _log) = (session, nav, log);
        MainPage = new AppShell();

        // The server rejected our token: go back to sign-in (always on the UI thread).
        _session.Expired += () => MainThread.BeginInvokeOnMainThread(async () => await GoToAsync("//login"));
    }

    protected override async void OnStart()
    {
        base.OnStart();
        await _session.RestoreAsync();
        if (_session.IsSignedIn) await GoToAsync("//budgets");
    }

    private async Task GoToAsync(string route)
    {
        try { await _nav.GoToAsync(route); }
        catch (Exception ex) { _log.LogError(ex, "Navigation to {Route} failed", route); }   // async void must not throw
    }
}
