using Kopeck.Domain.Users;

namespace Kopeck.Api.Tests;

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
