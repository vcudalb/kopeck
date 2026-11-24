using System.Net;

namespace Kopeck.Mobile.Services;

public sealed class ApiException(string message, HttpStatusCode status) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}
