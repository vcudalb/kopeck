using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kopeck.Api.Controllers;

/// <summary>Liveness probe for load balancers and uptime checks.</summary>
[AllowAnonymous]
[Route("health")]
public sealed class HealthController : ApiControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
