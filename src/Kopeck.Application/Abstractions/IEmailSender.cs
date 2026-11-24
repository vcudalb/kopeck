namespace Kopeck.Application.Abstractions;

/// <summary>Sends (or queues) a plain-text email.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
