namespace Kopeck.Application.Common;

/// <summary>Why a use case refused a request. The API maps each kind to one HTTP status code.</summary>
public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
}
