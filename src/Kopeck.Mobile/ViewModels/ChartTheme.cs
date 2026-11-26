using System.Runtime.CompilerServices;

namespace Kopeck.Mobile.ViewModels;

/// <summary>
/// Chart colours are picked for the light or dark theme when a chart is built, so a screen that is open while the theme
/// changes has to rebuild its charts. Screens register once; the handler list is weak, so a closed screen is not kept alive.
/// </summary>
public static class ChartTheme
{
    private static readonly ConditionalWeakTable<object, Action> Rebuilds = new();
    private static bool _hooked;

    public static void Subscribe(object owner, Action rebuild)
    {
        Rebuilds.AddOrUpdate(owner, rebuild);
        if (_hooked || Application.Current is null) return;
        _hooked = true;
        Application.Current.RequestedThemeChanged += (_, _) =>
        {
            foreach (var (_, action) in Rebuilds) MainThread.BeginInvokeOnMainThread(action);
        };
    }
}
