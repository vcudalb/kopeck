using Microsoft.Extensions.Logging;

namespace Kopeck.Infrastructure.Email;

/// <summary>Development only: writes the email to the log so you can read the code without an SMTP server.</summary>
public sealed class LogEmailTransport(ILogger<LogEmailTransport> log) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        log.LogWarning("DEV EMAIL to {To}: {Subject}\n{Body}", message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}
