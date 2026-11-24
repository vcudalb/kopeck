using System.Threading.RateLimiting;
using Kopeck.Api.Filters;
using Kopeck.Application.Abstractions;
using Kopeck.Contracts;
using Kopeck.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kopeck.Api.Http;

public static class WebApiSetup
{
    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        // [ServiceFilter(typeof(RequestValidationFilter<X>))] resolves the closed type from this open-generic registration.
        services.AddScoped(typeof(RequestValidationFilter<>));

        // ---- MVC controllers; JSON matches the mobile app (Kopeck.Contracts.Json) ----
        services.AddControllers(o =>
            {
                // Request validation belongs to the IRequestValidator<T> classes, not to implicit [Required] attributes.
                o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(o =>
            {
                foreach (var converter in Json.Options.Converters) o.JsonSerializerOptions.Converters.Add(converter);
            })
            .ConfigureApiBehaviorOptions(o =>
            {
                // Malformed bodies or missing route/query values: same { "error": "..." } shape as every other 400.
                o.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(new ApiError(string.Join(" ",
                    ctx.ModelState.Values.SelectMany(v => v.Errors).Select(e =>
                        string.IsNullOrWhiteSpace(e.ErrorMessage) ? "The request is not valid." : e.ErrorMessage).Distinct())));
            });

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
