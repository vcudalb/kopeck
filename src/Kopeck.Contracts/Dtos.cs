using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kopeck.Contracts;

/// <summary>Shared JSON settings so the API and the mobile app serialize identically.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = Create();

    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }
}

public enum BudgetRole { Viewer = 0, Editor = 1, Owner = 2 }
public enum ShareRole { Viewer = 0, Editor = 1 }
public enum CategoryStatus { Unused, OnTrack, Warning, Over, Unplanned }

// ---- Auth ----
public record RegisterRequest(string Email, string Password, string DisplayName);
public record EmailCodeRequest(string Email, string Code);
public record EmailRequest(string Email);
public record LoginRequest(string Email, string Password);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
public record AuthResponse(string Token, DateTimeOffset ExpiresAt, Guid UserId, string Email, string DisplayName);

// ---- Budgets (a "budget" is the dashboard that can be shared) ----
public record BudgetDto(Guid Id, string Name, string Currency, BudgetRole Role, string OwnerEmail);
public record BudgetRequest(string Name, string Currency);

// ---- Categories ----
public record CategoryDto(Guid Id, string Name, string Color);
public record CategoryRequest(string Name, string? Color);

// ---- Income ----
public record IncomeStreamDto(Guid Id, string Name);
public record IncomeStreamRequest(string Name);
public record IncomeDto(Guid Id, Guid StreamId, string StreamName, DateOnly Date, decimal Amount, string? Note);
public record IncomeRequest(Guid StreamId, DateOnly Date, decimal Amount, string? Note);

// ---- Expenses ----
public record ExpenseDto(Guid Id, Guid CategoryId, string CategoryName, DateOnly Date, decimal Amount, string? Note, string? CreatedBy);
public record ExpenseRequest(Guid CategoryId, DateOnly Date, decimal Amount, string? Note);

// ---- Plans (planned expense per category per month) ----
public record PlanDto(Guid CategoryId, string CategoryName, decimal Amount);
public record PlanRequest(Guid CategoryId, DateOnly Month, decimal Amount);
public record CopyPlanRequest(DateOnly FromMonth, DateOnly ToMonth);
public record CopyPlanResponse(int Copied);
/// <summary>Saves several categories' planned amounts for one month in a single transaction (all or nothing).</summary>
public record SavePlansRequest(DateOnly Month, IReadOnlyList<PlanAmount> Items);
public record PlanAmount(Guid CategoryId, decimal Amount);

// ---- Summary ----
public record CategorySummaryDto(
    Guid CategoryId, string Name, string Color,
    decimal Planned, decimal Spent,
    decimal Left,      // Planned - Spent (negative when overspent)
    decimal OverBy,    // how much more than planned was spent (0 if not over)
    decimal PercentUsed,
    CategoryStatus Status);

public record IncomeStreamSummaryDto(Guid StreamId, string Name, decimal Amount);

public record MonthSummaryDto(
    DateOnly Month, string Currency,
    decimal TotalIncome, decimal TotalPlanned, decimal TotalSpent,
    decimal TotalOverspend,   // sum of OverBy across categories
    decimal Unallocated,      // TotalIncome - TotalPlanned
    decimal NetLeft,          // TotalIncome - TotalSpent
    IReadOnlyList<CategorySummaryDto> Categories,
    IReadOnlyList<IncomeStreamSummaryDto> IncomeStreams);

// ---- Sharing ----
public record ShareDto(Guid Id, string Email, ShareRole Role);
public record ShareRequest(string Email, ShareRole Role);

public record ApiError(string Error);
