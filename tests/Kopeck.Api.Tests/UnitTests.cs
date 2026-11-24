using Kopeck.Domain.Budgets;
using Kopeck.Domain.Common;
using Kopeck.Domain.Planning;
using Kopeck.Domain.Users;
using Kopeck.Infrastructure.Email;

namespace Kopeck.Api.Tests;

public class PlanVsActualTests
{
    [Theory]
    [InlineData(0, 0, SpendingStatus.Unused)]
    [InlineData(0, 10, SpendingStatus.Unplanned)]
    [InlineData(100, 50, SpendingStatus.OnTrack)]
    [InlineData(100, 80, SpendingStatus.Warning)]
    [InlineData(100, 100, SpendingStatus.Warning)]
    [InlineData(100, 100.01, SpendingStatus.Over)]
    public void Maps_planned_and_spent_to_a_status(decimal planned, decimal spent, SpendingStatus expected) =>
        Assert.Equal(expected, new PlanVsActual(planned, spent).Status);

    [Fact]
    public void Left_is_negative_and_over_by_positive_when_overspent()
    {
        var line = new PlanVsActual(Planned: 3000, Spent: 3500);
        Assert.Equal((-500m, 500m, 116.7m), (line.Left, line.OverBy, line.PercentUsed));
    }

    [Fact]
    public void Unplanned_spending_counts_fully_as_over()
    {
        var line = new PlanVsActual(Planned: 0, Spent: 250);
        Assert.Equal((250m, 100m), (line.OverBy, line.PercentUsed));
    }
}

public class UserCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Email_code_confirms_once_and_is_stored_hashed()
    {
        var user = User.Create("a@test.dev", Now);
        var code = user.IssueVerifyCode(Now);
        Assert.NotEqual(code, user.VerifyCodeHash);

        Assert.True(user.TryConfirmEmail(code, Now.AddMinutes(1)));
        Assert.True(user.EmailConfirmed);
        Assert.False(user.TryConfirmEmail(code, Now.AddMinutes(2)));   // single use
    }

    [Fact]
    public void Code_expires_after_15_minutes()
    {
        var user = User.Create("a@test.dev", Now);
        var code = user.IssueVerifyCode(Now);
        Assert.False(user.TryConfirmEmail(code, Now.AddMinutes(16)));
    }

    [Fact]
    public void Code_is_locked_after_five_wrong_guesses_even_if_the_right_one_follows()
    {
        var user = User.Create("a@test.dev", Now);
        var code = user.IssueVerifyCode(Now);
        for (var i = 0; i < User.MaxCodeAttempts; i++) Assert.False(user.TryConfirmEmail("000000" == code ? "111111" : "000000", Now));
        Assert.False(user.TryConfirmEmail(code, Now));
    }

    [Fact]
    public void Account_locks_after_repeated_failed_logins_then_unlocks()
    {
        var user = User.Create("a@test.dev", Now);
        for (var i = 0; i < User.MaxFailedLogins; i++) user.RegisterFailedLogin(Now);
        Assert.True(user.IsLockedOut(Now.AddMinutes(5)));
        Assert.False(user.IsLockedOut(Now.AddMinutes(16)));
    }
}

public class PasswordPolicyTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("short1", false)]
    [InlineData("alllettersnodigit", false)]
    [InlineData("12345678901", false)]
    [InlineData("Passw0rd!x", true)]
    public void Enforces_length_letters_and_digits(string? password, bool valid) =>
        Assert.Equal(valid, PasswordPolicy.Validate(password) is null);
}

public class DomainRulesTests
{
    [Theory]
    [InlineData(" Ana@Example.COM ", "ana@example.com")]
    [InlineData("not-an-email", null)]
    [InlineData("Ana <ana@example.com>", null)]   // display-name form is not a plain address
    public void Normalizes_or_rejects_emails(string input, string? expected) =>
        Assert.Equal(expected, DomainRules.NormalizeEmail(input));

    [Theory]
    [InlineData("eur", "EUR")]
    [InlineData("E1R", null)]
    [InlineData("EU", null)]
    public void Normalizes_or_rejects_currencies(string input, string? expected) =>
        Assert.Equal(expected, DomainRules.NormalizeCurrency(input));
}

public class BudgetAccessTests
{
    [Fact]
    public void Owner_share_and_stranger_get_the_right_access()
    {
        var owner = Guid.NewGuid();
        var budget = new Budget("Home", "EUR", owner);
        var share = new BudgetShare(budget.Id, "friend@test.dev", AccessLevel.Editor);
        var otherBudgetShare = new BudgetShare(Guid.NewGuid(), "friend@test.dev", AccessLevel.Editor);

        Assert.Equal(AccessLevel.Owner, budget.AccessFor(owner, null));
        Assert.Equal(AccessLevel.Editor, budget.AccessFor(Guid.NewGuid(), share));
        Assert.Null(budget.AccessFor(Guid.NewGuid(), otherBudgetShare));
        Assert.Null(budget.AccessFor(Guid.NewGuid(), null));
    }

    [Fact]
    public void Ownership_cannot_be_shared() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BudgetShare(Guid.NewGuid(), "x@test.dev", AccessLevel.Owner));
}

public class EmailLogMaskingTests
{
    [Theory]
    [InlineData("ana@example.com", "a***@example.com")]
    [InlineData("a@example.com", "***@example.com")]
    [InlineData("no-at-sign", "***")]
    public void Recipient_is_masked_in_logs(string email, string expected) =>
        Assert.Equal(expected, EmailDispatchWorker.Mask(email));
}
