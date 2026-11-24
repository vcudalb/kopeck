using System.Threading.Channels;
using Kopeck.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kopeck.Infrastructure.Email;

/// <summary>
/// Producer side: requests only enqueue, they never wait for SMTP. That keeps response times identical whether or not
/// an email is actually sent (no account enumeration by timing) and stops a slow mail server from slowing sign-up.
/// </summary>
public sealed class QueuedEmailSender(Channel<EmailMessage> queue) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default) =>
        queue.Writer.WriteAsync(new EmailMessage(to, subject, body), ct).AsTask();
}

/// <summary>Consumer side: delivers queued emails one at a time through the configured <see cref="IEmailTransport"/>.</summary>
public sealed class EmailDispatchWorker(Channel<EmailMessage> queue, IEmailTransport transport, ILogger<EmailDispatchWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await transport.SendAsync(message, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The recipient is masked: email addresses are personal data and don't belong in logs.
                log.LogError(ex, "Email '{Subject}' to {Recipient} could not be sent", message.Subject, Mask(message.To));
            }
        }
    }

    internal static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at switch
        {
            < 0 => "***",
            <= 1 => "***" + email[at..],
            _ => $"{email[0]}***{email[at..]}",
        };
    }
}
