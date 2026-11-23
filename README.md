# Kopeck (.NET MAUI + ASP.NET Core)

Replaces the "Main Buget.xlsx" workflow: categories with a monthly plan, "Left" per category, multiple income streams, and sharing.

## Excel → app mapping

| Excel | App |
|---|---|
| Summary sheet, `Planned` column per category | **Plan** screen (planned amount per category per month) |
| `Left` = `Planned - SUMIF(expenses by category)` | Dashboard "Left" per category, plus **"Over by X"** when spending exceeds plan |
| Expense list (Amount / Category) | **Expenses** screen: each expense is assigned to a category |
| Details sheet: Salaries / Other, monthly columns | **Income** screen: any number of revenue streams, entries dated per month |
| Duplicating last month's sheet | "Copy missing amounts from previous month" on the Plan screen |
| Sharing the file | **Share dashboard**: invite by email as Viewer or Editor |

Not carried over (kept simple on purpose): the MDL/EUR savings & conversion sheet (a budget has one currency) and the "weekly planning" split of Home into W1–W5.

## Stack (all open source)

- **Client**: .NET 8 MAUI (Android + iOS), MVVM with CommunityToolkit.Mvvm. JWT is stored in SecureStorage.
- **API**: ASP.NET Core 8 minimal API, EF Core + SQLite (swap provider for PostgreSQL in production), JWT bearer auth, ASP.NET Identity `PasswordHasher` (PBKDF2), MailKit for SMTP.
- **Shared**: `Kopeck.Contracts` (DTOs used by both sides).
- **Tests**: xUnit + `WebApplicationFactory` (auth flow, planned-vs-real maths, sharing permissions).

## Accounts and email

Register → a 6-digit code is emailed → verify → signed in. Also: login, resend code, forgot/reset password. Codes expire after 15 minutes, are stored hashed, and lock after 5 wrong tries; auth endpoints are rate limited.

## Run it

```powershell
# API (Development: the verification code is printed in the console instead of emailed)
dotnet run --project src/Kopeck.Api --urls http://0.0.0.0:5080

# Tests
dotnet test tests/Kopeck.Api.Tests
```

Production configuration (environment variables, never commit them):
`Jwt__Key` (random 32+ bytes), `Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`, `Smtp__From`, `ConnectionStrings__Default`. The API refuses to start outside Development without `Jwt__Key` and `Smtp__Host`. Serve it over HTTPS.

Mobile app (needs the MAUI workload; iOS also needs a Mac with Xcode):

```powershell
dotnet workload install maui
dotnet build src/Kopeck.Mobile -f net8.0-android -t:Run
```

The server address is editable on the sign-in screen (default `http://10.0.2.2:5080` for the Android emulator, `http://localhost:5080` for the iOS simulator). Plain HTTP is only allowed in Debug builds on Android (`MainApplication.cs`) and Release builds reject non-https server addresses. iOS `Info.plist` still allows local-network HTTP; remove it for releases.

## Sharing model

A budget is the dashboard. The owner shares it by email with role **Viewer** (read-only) or **Editor** (add expenses, income, plans, categories). Shares are keyed by verified email, so inviting someone who hasn't registered yet works once they sign up with that address. Only the owner can manage shares or delete the budget; a shared user can leave. People without access get 404.

## Architecture

```
src/Kopeck.Contracts   DTOs + JSON settings shared by API and app (single source of truth for the wire format)
src/Kopeck.Api
  Data/                EF Core entities, DbContext (FKs with cascade, indexes, precision), Migrations/
  Auth/                JWT, password hashing, SMTP/console email senders
  Endpoints/           one file per feature; BudgetAccess.cs authorizes every /api/budgets/{budgetId}/... route in one filter
  Services/            SummaryService (planned vs real), error handling
src/Kopeck.Mobile
  Services/            ApiClient (no UI knowledge), Session, AppState, Navigation/Dialog abstractions
  ViewModels/          MVVM; no direct Shell/UI calls, so they can be unit tested
  Views/               XAML with compiled bindings (x:DataType)
```

- Authorization: one endpoint filter loads the caller's role for the budget; endpoints declare the minimum role (`RequireRole(Editor)`). Callers without access get 404.
- Rules worth knowing: user lockout after 5 bad passwords (15 min), email codes are single-use/hashed/expire, amounts max 2 decimals, unique names per budget (races return 409).
- Database: EF migrations. `dotnet dotnet-ef migrations add <Name> --project src/Kopeck.Api --output-dir Data/Migrations` after model changes. Migrations auto-apply in Development; elsewhere run `dotnet dotnet-ef database update` on deploy, or set `Database__MigrateOnStartup=true` for a single instance.
- Behind a proxy set `ForwardedHeaders__Enabled=true` so rate limiting sees client IPs.
- Quality gates: `Directory.Build.props` turns warnings into errors for the API/contracts/tests; `.editorconfig` sets style.

## Known gaps / next steps

- The mobile project was written but **not compiled or run** (the MAUI workload is installed on the dev machine, but the JDK and Android SDK are not, so it was never built). Expect small XAML/binding fixes on first build. No app icon or splash set yet.
- No refresh tokens (JWT lasts 7 days, configurable with `Jwt__LifetimeHours`); no per-request cancellation wiring in view models yet.
- Single currency per budget; no recurring expenses, charts, or import of the existing xlsx.
