using System.Reflection;

namespace Kopeck.Api.Http;

/// <summary>
/// One feature's routes. Modules are discovered at startup, so adding a feature means adding a class,
/// not editing Program.cs (open/closed).
/// </summary>
public interface IEndpointModule
{
    void Map(IEndpointRouteBuilder app);
}

public static class EndpointModuleExtensions
{
    public static IEndpointRouteBuilder MapEndpointModules(this IEndpointRouteBuilder app)
    {
        var modules = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IEndpointModule).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .Select(t => (IEndpointModule)Activator.CreateInstance(t)!);

        foreach (var module in modules) module.Map(app);
        return app;
    }
}
