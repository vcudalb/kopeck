namespace Kopeck.Api.Http;

/// <summary>
/// One feature's routes. Modules are discovered at startup, so adding a feature means adding a class,
/// not editing Program.cs (open/closed).
/// </summary>
public interface IEndpointModule
{
    void Map(IEndpointRouteBuilder app);
}
