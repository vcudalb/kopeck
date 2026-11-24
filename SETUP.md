# Local setup on Windows (no Android Studio)

How to run the Kopeck API and the Android app on an emulator, using only the .NET CLI, Google's Android command-line
tools and JetBrains Rider. Android Studio isn't needed.

Run the commands in PowerShell. Steps marked **(admin)** need an elevated PowerShell.

## 1. Prerequisites

| What | Version | Check |
|---|---|---|
| .NET SDK | 8.0.428 (pinned in `global.json`) | `dotnet --list-sdks` |
| JDK | 17 (Microsoft Build of OpenJDK) | `java -version` |
| Rider | any recent version with .NET MAUI support | |

If the JDK is missing:

```powershell
winget install Microsoft.OpenJDK.17
```

## 2. Install the MAUI Android workload (admin)

```powershell
dotnet workload install maui-android
```

## 3. Install the Android SDK with dotnet

The .NET Android build has a target that downloads the parts the project needs (platform, build-tools,
platform-tools, cmdline-tools) into a folder you choose. This guide uses `C:\Android\sdk`.

```powershell
$jdk = (Get-ChildItem "C:\Program Files\Microsoft" -Directory -Filter "jdk-17*" | Select-Object -First 1).FullName
```

```powershell
dotnet build C:\Code\kopeck\src\Kopeck.Mobile -t:InstallAndroidDependencies -f net8.0-android -p:AndroidSdkDirectory=C:\Android\sdk -p:JavaSdkDirectory="$jdk" -p:AcceptAndroidSDKLicenses=True
```

Save the paths as user environment variables so Rider and `dotnet build` can find them:

```powershell
[Environment]::SetEnvironmentVariable("ANDROID_HOME", "C:\Android\sdk", "User")
```

```powershell
[Environment]::SetEnvironmentVariable("JAVA_HOME", $jdk, "User")
```

> **Restart your terminals and Rider after this step.** Apps that were already open, including terminal panels
> inside an IDE, keep their old environment. Without a restart the Android build fails with `XA5300`.

## 4. Create the emulator

The command-line tools are installed in a **versioned** folder (for example `cmdline-tools\11.0`), not in
`cmdline-tools\latest`. Find the folder first:

```powershell
$tools = (Get-ChildItem C:\Android\sdk\cmdline-tools -Directory | Select-Object -First 1).FullName + "\bin"
```

Install the emulator and an Android 14 (API 34) system image. Type `y` if it asks you to accept a license.

```powershell
& "$tools\sdkmanager.bat" "emulator" "system-images;android-34;google_apis;x86_64"
```

Create a virtual device named `kopeck`. Press Enter if it asks about a custom hardware profile.

```powershell
& "$tools\avdmanager.bat" create avd -n kopeck -k "system-images;android-34;google_apis;x86_64" -d pixel_6
```

### Hardware acceleration (admin, once)

The emulator needs the Windows Hypervisor Platform. Turn it on and reboot. On a managed laptop this may need IT.

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName HypervisorPlatform
```

Check it. The output should say `WHPX ... is installed and usable`.

```powershell
C:\Android\sdk\emulator\emulator.exe -accel-check
```

## 5. Configure Rider (once)

1. **Settings → Build, Execution, Deployment → Android:** set the Android SDK to `C:\Android\sdk` and the JDK to the
   Microsoft JDK 17 folder.
2. Open `Kopeck.sln`. Rider creates an Android run configuration for `Kopeck.Mobile`.
3. For backend-only work, open `Kopeck.Backend.slnf` instead. It doesn't need the Android SDK.

## 6. Run everything

### 6.1 Start the emulator (terminal 1)

```powershell
C:\Android\sdk\emulator\emulator.exe -avd kopeck
```

Wait for the Android home screen. This should list `emulator-5554   device`:

```powershell
C:\Android\sdk\platform-tools\adb.exe devices
```

### 6.2 Start the API (terminal 2)

Run it **from the repository root**, so `global.json` selects .NET 8.

```powershell
cd C:\Code\kopeck
```

```powershell
dotnet run --project src\Kopeck.Api --urls http://0.0.0.0:5080
```

In Development the API:

- creates and migrates the SQLite database `kopeck.db` in the current folder on start;
- writes emails, including the 6-digit verification codes, to this console instead of sending them;
- uses a random sign-in key on each start, so you have to sign in again after a restart.

Check that it's up. `401` means it's running (the endpoint needs a sign-in):

```powershell
curl.exe -s -o NUL -w "%{http_code}" http://localhost:5080/api/budgets
```

### 6.3 Run the app

In Rider, pick the **Kopeck.Mobile** configuration and the **emulator-5554** device, then press Run or Debug.

From the command line instead (repository root):

```powershell
dotnet build src\Kopeck.Mobile -f net8.0-android -t:Run
```

The first build and deploy take a few minutes.

## 7. Try it

1. On the sign-in screen, keep the server address `http://10.0.2.2:5080`. That's your PC's `localhost` as seen from
   the emulator.
2. **Register with a made-up address** such as `alice@example.com` and a password of at least 8 characters. No email
   is sent locally, so don't use a real address.
3. Copy the 6-digit code from the API console (6.2) and verify.
4. Create a budget, then try categories, plan, expenses, income and the dashboard.
5. To test sharing, register a second account (for example `bob@example.com`), share a budget with it as Viewer or
   Editor, and sign in as that user.

## Troubleshooting

| Problem | Fix |
|---|---|
| `sdkmanager.bat is not recognized` | The tools are in a versioned folder such as `cmdline-tools\11.0\bin`, not `latest\bin`. See step 4. |
| `XA5300: The Android SDK directory could not be found` | The terminal or Rider started before `ANDROID_HOME` was saved. Restart it, or run `$env:ANDROID_HOME = "C:\Android\sdk"` in the current terminal, or add `-p:AndroidSdkDirectory=C:\Android\sdk` to the build. |
| `IDE0161` in a migration file | Fixed in `.editorconfig` by marking `**/Migrations/*.cs` as generated code. Pull the latest changes. |
| The API builds with the wrong SDK | Run `dotnet` from the repository root so `global.json` applies (`dotnet --version` should print `8.0.428`). |
| "Can't reach the server" in the app | Make sure the API is running on port 5080, and allow `dotnet` through Windows Firewall on private networks. If you run the API from Rider's `http` profile it listens on port 5257; change the server address on the sign-in screen to `http://10.0.2.2:5257`. |
| Rider doesn't list the emulator | Run `adb.exe devices` (6.1). If nothing is listed, start the emulator again. |
| Signed out after restarting the API | Expected in Development (random sign-in key). Your data stays in `kopeck.db`. |
| Start over with an empty database | Stop the API and delete `kopeck.db` in the repository root. |
| `git: detected dubious ownership` | The folder is owned by another account (for example after cloning as administrator). If you trust it: `git config --global --add safe.directory C:/Code/kopeck` |
