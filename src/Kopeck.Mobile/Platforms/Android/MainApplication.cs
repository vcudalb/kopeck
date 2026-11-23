using Android.App;
using Android.Runtime;

namespace Kopeck.Mobile;

// Plain-HTTP traffic (to a dev API on the emulator host) is only allowed in Debug builds.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
