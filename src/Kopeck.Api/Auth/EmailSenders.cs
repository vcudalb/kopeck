using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Kopeck.Api.Auth;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>SMTP via MailKit (open source). Configure Smtp:Host/Port/User/Password/From through env vars or secrets.</summary>
public sealed class SmtpEmailSender(IConfiguration cfg) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(cfg["Smtp:From"] ?? "no-reply@localhost"));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(cfg["Smtp:Host"]!, cfg.GetValue("Smtp:Port", 587), SecureSocketOptions.StartTls, ct);
        if (!string.IsNullOrEmpty(cfg["Smtp:User"]))
            await client.AuthenticateAsync(cfg["Smtp:User"]!, cfg["Smtp:Password"] ?? "", ct);
        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);
    }
}

/// <summary>Development only: writes the email to the log so you can read the code without an SMTP server.</summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> log) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        log.LogWarning("DEV EMAIL to {To}: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
