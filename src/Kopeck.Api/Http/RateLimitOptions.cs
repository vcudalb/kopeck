namespace Kopeck.Api.Http;

public sealed class RateLimitOptions
{
    public const string Section = "RateLimit";
    public const string AuthPolicy = "auth";

    /// <summary>Requests per minute per client IP on the /api/auth endpoints.</summary>
    public int AuthPerMinute { get; set; } = 20;
}
