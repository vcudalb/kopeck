using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Kopeck.Infrastructure.Email;

public sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>How an email actually leaves the process (strategy: SMTP in real environments, the log in Development).</summary>
public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@localhost";
}

/// <summary>SMTP via MailKit (open source). Configure Smtp:Host/Port/User/Password/From through env vars or secrets.</summary>
public sealed class SmtpEmailTransport(IOptions<SmtpOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var o = options.Value;
        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(o.From));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("plain") { Text = message.Body };

        using var client = new SmtpClient();
        await client.ConnectAsync(o.Host ?? throw new InvalidOperationException("Smtp:Host is not configured."), o.Port, SecureSocketOptions.StartTls, ct);
        if (!string.IsNullOrEmpty(o.User)) await client.AuthenticateAsync(o.User, o.Password ?? "", ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }
}

/// <summary>Development only: writes the email to the log so you can read the code without an SMTP server.</summary>
public sealed class LogEmailTransport(ILogger<LogEmailTransport> log) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        log.LogWarning("DEV EMAIL to {To}: {Subject}\n{Body}", message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}
