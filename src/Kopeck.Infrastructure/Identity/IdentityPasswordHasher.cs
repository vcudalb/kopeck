using Kopeck.Domain.Users;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = Kopeck.Application.Abstractions.IPasswordHasher;

namespace Kopeck.Infrastructure.Identity;

/// <summary>PBKDF2 via ASP.NET Identity's hasher (salted, versioned, upgrades iteration counts over time). Adapter over <see cref="PasswordHasher{TUser}"/>.</summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // Verified against when the account doesn't exist, so response time doesn't reveal which emails are registered.
    private readonly string _dummyHash;
    private readonly User _dummyUser = User.Create("dummy@invalid", DateTimeOffset.UnixEpoch);

    public IdentityPasswordHasher() => _dummyHash = _hasher.HashPassword(_dummyUser, Guid.NewGuid().ToString("N"));

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User? user, string password)
    {
        if (user is null)
        {
            _hasher.VerifyHashedPassword(_dummyUser, _dummyHash, password);
            return false;
        }
        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
    }
}
