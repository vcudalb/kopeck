using System.Threading.RateLimiting;
using Kopeck.Api.Auth;
using Kopeck.Api.Data;
using Kopeck.Api.Endpoints;
using Kopeck.Api.Services;
using Kopeck.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

services.AddDbContext<KopeckDb>(o => o.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=kopeck.db"));
services.AddSingleton(TimeProvider.System);
services.AddScoped<ISummaryService, SummaryService>();

// ---- authentication ----
services.AddSingleton<JwtService>();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtService>((o, jwt) => { o.TokenValidationParameters = jwt.ValidationParameters; o.MapInboundClaims = false; });
services.AddAuthorization();

// Real SMTP when Smtp:Host is set; log-to-console only in Development.
if (!string.IsNullOrEmpty(config["Smtp:Host"])) services.AddSingleton<IEmailSender, SmtpEmailSender>();
else if (builder.Environment.IsDevelopment()) services.AddSingleton<IEmailSender, ConsoleEmailSender>();
else throw new InvalidOperationException("Smtp:Host must be configured outside Development.");

// Brute-force protection for register/login/code endpoints (per client IP).
services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = ctx.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimit:AuthPerMinute", 20),
            Window = TimeSpan.FromMinutes(1),
        }));
});

// Behind a reverse proxy/load balancer, enable this so rate limiting sees the real client IP (and HTTPS scheme).
if (config.GetValue<bool>("ForwardedHeaders:Enabled"))
    services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

services.ConfigureHttpJsonOptions(o => { foreach (var c in Json.Options.Converters) o.SerializerOptions.Converters.Add(c); });
services.AddExceptionHandler<UniqueConstraintExceptionHandler>();
services.AddProblemDetails();

var app = builder.Build();

// Schema is created from EF migrations. Auto-apply in Development/tests; in production run `dotnet ef database update`
// as a deployment step, or set Database:MigrateOnStartup=true for single-instance setups.
if (app.Environment.IsDevelopment() || config.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<KopeckDb>().Database.Migrate();
}

if (config.GetValue<bool>("ForwardedHeaders:Enabled")) app.UseForwardedHeaders();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapAuth();
app.MapBudgets();
app.MapCategories();
app.MapIncome();
app.MapExpenses();
app.MapPlans();

app.Run();

public partial class Program;
