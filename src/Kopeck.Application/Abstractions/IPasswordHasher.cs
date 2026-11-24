using Kopeck.Domain.Users;

namespace Kopeck.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(User user, string password);

    /// <summary>Returns false for a null user, after spending the same time as a real check (no account enumeration by timing).</summary>
    bool Verify(User? user, string password);
}
