namespace Kopeck.Infrastructure.Email;

/// <summary>How an email actually leaves the process (strategy: SMTP in real environments, the log in Development).</summary>
public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
