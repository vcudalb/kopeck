using Kopeck.Infrastructure.Email;

namespace Kopeck.Api.Tests;

public class EmailLogMaskingTests
{
    [Theory]
    [InlineData("ana@example.com", "a***@example.com")]
    [InlineData("a@example.com", "***@example.com")]
    [InlineData("no-at-sign", "***")]
    public void Recipient_is_masked_in_logs(string email, string expected) =>
        Assert.Equal(expected, EmailDispatchWorker.Mask(email));
}
