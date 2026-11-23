using Kopeck.Api.Auth;
using Kopeck.Api.Data;
using Kopeck.Api.Services;
using Kopeck.Contracts;

namespace Kopeck.Api.Tests;

public class CategoryStatusRuleTests
{
    [Theory]
    [InlineData(0, 0, CategoryStatus.Unused)]
    [InlineData(0, 10, CategoryStatus.Unplanned)]
    [InlineData(100, 50, CategoryStatus.OnTrack)]
    [InlineData(100, 80, CategoryStatus.Warning)]
    [InlineData(100, 100, CategoryStatus.Warning)]
    [InlineData(100, 100.01, CategoryStatus.Over)]
    public void Maps_planned_and_spent_to_a_status(decimal planned, decimal spent, CategoryStatus expected) =>
        Assert.Equal(expected, CategoryStatusRule.For(planned, spent));
}

public class UserCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Email_code_confirms_once_and_is_stored_hashed()
    {
        var user = new User { Email = "a@test.dev" };
        var code = user.IssueVerifyCode(Now);
        Assert.NotEqual(code, user.VerifyCodeHash);

        Assert.True(user.TryConfirmEmail(code, Now.AddMinutes(1)));
        Assert.True(user.EmailConfirmed);
        Assert.False(user.TryConfirmEmail(code, Now.AddMinutes(2)));   // single use
    }

    [Fact]
    public void Code_expires_after_15_minutes()
    {
        var user = new User();
        var code = user.IssueVerifyCode(Now);
        Assert.False(user.TryConfirmEmail(code, Now.AddMinutes(16)));
    }

    [Fact]
    public void Code_is_locked_after_five_wrong_guesses_even_if_the_right_one_follows()
    {
        var user = new User();
        var code = user.IssueVerifyCode(Now);
        for (var i = 0; i < User.MaxCodeAttempts; i++) Assert.False(user.TryConfirmEmail("000000" == code ? "111111" : "000000", Now));
        Assert.False(user.TryConfirmEmail(code, Now));
    }

    [Fact]
    public void Account_locks_after_repeated_failed_logins_then_unlocks()
    {
        var user = new User();
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
