using System.Threading.RateLimiting;
using Kopeck.Application.Abstractions;
using Kopeck.Contracts;
using Kopeck.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Kopeck.Api.Http;

public sealed class RateLimitOptions
{
    public const string Section = "RateLimit";
    public const string AuthPolicy = "auth";

    /// <summary>Requests per minute per client IP on the /api/auth endpoints.</summary>
    public int AuthPerMinute { get; set; } = 20;
}

public static class WebApiSetup
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        // ---- authentication: JWT bearer, validated with the same key that signs tokens ----
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtTokenService>((o, jwt) =>
            {
                o.TokenValidationParameters = jwt.ValidationParameters;
                o.MapInboundClaims = false;
            });
        services.AddAuthorization();

        // ---- brute-force protection for register/login/code endpoints (per client IP) ----
        services.Configure<RateLimitOptions>(config.GetSection(RateLimitOptions.Section));
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(RateLimitOptions.AuthPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = ctx.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value.AuthPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                }));
        });

        // Behind a reverse proxy/load balancer, enable this so rate limiting sees the real client IP (and HTTPS scheme).
        if (config.GetValue<bool>("ForwardedHeaders:Enabled"))
            services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        services.ConfigureHttpJsonOptions(o =>
        {
            foreach (var converter in Json.Options.Converters) o.SerializerOptions.Converters.Add(converter);
        });
        services.AddExceptionHandler<UniqueConstraintExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }

    public static WebApplication UseWebApi(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled")) app.UseForwardedHeaders();
        app.UseExceptionHandler();
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
