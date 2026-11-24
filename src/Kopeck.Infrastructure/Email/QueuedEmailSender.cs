using System.Threading.Channels;
using Kopeck.Application.Abstractions;

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
