using Kopeck.Domain.Users;

namespace Kopeck.Application.Abstractions;

public interface ITokenService
{
    AccessToken Create(User user);
}
