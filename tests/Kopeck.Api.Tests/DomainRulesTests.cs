using Kopeck.Domain.Common;

namespace Kopeck.Api.Tests;

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
