using System.Text.RegularExpressions;
using Kopeck.Application.Abstractions;

namespace Kopeck.Api.Tests;

public class CapturingEmail : IEmailSender
{
    public readonly List<(string To, string Body)> Sent = [];
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    { lock (Sent) Sent.Add((to, body)); return Task.CompletedTask; }
    public string LastCode(string to) { lock (Sent) return Regex.Match(Sent.Last(m => m.To == to).Body, @"\d{6}").Value; }
}
