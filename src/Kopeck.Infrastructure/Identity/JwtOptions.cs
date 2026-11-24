namespace Kopeck.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>Signing secret (32+ bytes). Required outside Development; set through the Jwt__Key environment variable or a secret store.</summary>
    public string? Key { get; set; }

    public int LifetimeHours { get; set; } = 24 * 7;
}
