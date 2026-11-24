using Kopeck.Domain.Users;

namespace Kopeck.Api.Tests;

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
