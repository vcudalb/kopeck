# Kopeck: requirements and implementation status

Source: the request to replace the "Main Buget.xlsx" workflow with a mobile app.
Status key: ✅ implemented and covered by automated tests · 🟡 implemented, not verified end to end · ❌ not implemented

## 1. Requested requirements

| # | Requirement | Status | How it is met |
|---|---|---|---|
| R1 | MAUI app for Android and iOS | 🟡 | .NET 8 MAUI project (`src/Kopeck.Mobile`). The Android build passes in CI, and the app runs on the Android emulator (API 34), where the main flows were checked by hand. Not yet run on a real Android phone. The iOS build has never been run and needs a Mac with Xcode. |
| R2 | Create new categories | ✅ | Categories screen: add, rename and delete. Names are unique per budget. A category with expenses cannot be deleted (409). Each person can star categories so they come first. |
| R3 | Assign categories to expenses | ✅ | Each expense requires a category. Expenses screen: add, edit and delete. |
| R4 | Multiple revenue streams | ✅ | Income screen: any number of named streams, with income entries dated per month. |
| R5 | See how much money is left per category | ✅ | Dashboard shows Planned, Spent and Left (Planned − Spent) per category for the chosen month. |
| R6 | Planned expenses compared with real data; when over plan, by how much | ✅ | Plan screen sets a planned amount per category per month. The dashboard shows "Over by X" and a status per category: Unused, On track, Warning (80% or more used), Over, Unplanned. |
| R7 | Registration and login with email | ✅ | Register, then a 6-digit emailed code, then verify and sign in. Also login, resend code, forgot password and reset password. |
| R8 | Open-source packages only | ✅ | Every dependency is open source and free to use. |
| R9 | Share a view/dashboard with another user | ✅ | A budget is the dashboard. The owner invites people by email as Viewer (read-only) or Editor (can change data). Only the owner can manage shares or delete the budget. A shared user can leave. People without access get 404. |

## 2. How the Excel file maps to the app

| Excel | App |
|---|---|
| Summary sheet, Planned column per category | Plan screen |
| Left = Planned − sum of expenses per category | Dashboard "Left" and "Over by" |
| Expense list (Amount, Category) | Expenses screen |
| Details sheet: Salaries and Other income by month | Income screen with multiple streams |
| MDL/EUR savings sheet | Savings tab: pots with their own currency, target and history |
| (new) charts | Overview spending split; Year screen with monthly bars and per-category totals |
| Duplicating last month's sheet | "Copy missing amounts from previous month" on the Plan screen |
| Sharing the file | Share dashboard |

Not carried over, on purpose: currency conversion (amounts in different currencies are listed side by side, never converted) and the weekly split of Home into W1–W5.

## 3. Account and security behaviour (implemented)

- Verification and reset codes expire after 15 minutes. They are stored hashed, are single-use and lock after 5 wrong tries.
- An account locks for 15 minutes after 5 wrong passwords. Login takes the same time whether or not the email is registered.
- Register, resend code and forgot password always reply the same way, so they don't reveal which emails are registered.
- Passwords are hashed with ASP.NET Identity PBKDF2. The sign-in token (JWT) lasts 7 days by default and is kept in SecureStorage on the phone.
- Auth endpoints are rate limited.
- Amounts allow at most 2 decimals and are stored with fixed precision.

## 4. Technology

| Part | Choice |
|---|---|
| App | .NET 8 MAUI, MVVM with CommunityToolkit.Mvvm. Shell navigation: sign-in → budget list → bottom tabs (Overview, Expenses, Income, Savings, More); the monthly plan is pushed from Overview and More |
| API | ASP.NET Core 8 Web API with controllers, EF Core with SQLite and migrations |
| Auth and email | JWT bearer tokens, MailKit over SMTP |
| Shared | `Kopeck.Contracts` (DTOs used by app and API) |
| Tests | xUnit with `WebApplicationFactory` |
| CI | GitHub Actions: API build and tests, Android build; iOS build is manual |

## 5. Verification status

- ✅ **API:** 39 automated tests pass. They cover sign-up, login, lockout, password reset, the planned-vs-real maths, sharing roles, cascade delete and decimal validation.
- ✅ **Android:** the app compiles in CI.
- ✅ **Android emulator:** checked by hand on 2026-11-24 on an API 34 emulator against a local API: registration with the emailed code, sign-in, and the budget screens work as required. Setup steps are in [SETUP.md](SETUP.md).
- 🟡 **Not tested:** a real Android phone, and automated UI tests (there are none).
- ❌ **iOS:** never built or run.

## 6. Known gaps

- ❌ No refresh tokens. The 7-day token has no renewal.
- ❌ No request cancellation from the view models.
- ❌ No currency conversion. Expenses and plans use the budget currency; income streams and savings pots can use others, shown separately.
- ❌ No recurring expenses, charts, or import of the existing xlsx.
- ❌ The iOS `Info.plist` still allows local HTTP. Remove that before a release.
- ❌ The app ID `com.example.kopeck` is a placeholder and must be changed before publishing.
