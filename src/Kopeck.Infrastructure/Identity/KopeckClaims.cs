namespace Kopeck.Infrastructure.Identity;

/// <summary>Claim names shared by token creation (here) and the web layer that reads them.</summary>
public static class KopeckClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
}
