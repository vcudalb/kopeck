using Kopeck.Domain.Common;

namespace Kopeck.Application.Validation;

/// <summary>Messages shared by several validators, so the same mistake reads the same everywhere.</summary>
public static class ValidationMessages
{
    public const string InvalidEmail = "Enter a valid email address.";
    public const string InvalidCode = "Enter the 6-digit code from the email.";
    public const string PositiveAmount = "Amount must be greater than 0 (max 2 decimals).";
    public const string PlannedAmount = "Planned amount must be 0 or more (max 2 decimals).";
    public const string CategoryRequired = "Choose a category.";

    public static string NameRequired(string what) => $"Enter a {what} name (max {DomainRules.MaxNameLength} characters).";
}
