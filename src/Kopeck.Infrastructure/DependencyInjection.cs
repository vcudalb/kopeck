using System.Threading.Channels;
using Kopeck.Application.Abstractions;
using Kopeck.Infrastructure.Email;
using Kopeck.Infrastructure.Identity;
using Kopeck.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kopeck.Infrastructure;

public static class DependencyInjection
{
    private const int EmailQueueCapacity = 500;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.AddSingleton(TimeProvider.System);

        // ---- persistence ----
        services.AddDbContext<KopeckDb>(o => o.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=kopeck.db"));
        services.AddScoped<IKopeckDbContext>(sp => sp.GetRequiredService<KopeckDb>());

        // ---- identity ----
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<ITokenService>(sp => sp.GetRequiredService<JwtTokenService>());
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        // ---- email: queued, sent in the background over SMTP (or to the log in Development) ----
        services.Configure<SmtpOptions>(config.GetSection(SmtpOptions.Section));
        var smtpHost = config[$"{SmtpOptions.Section}:{nameof(SmtpOptions.Host)}"];
        if (!string.IsNullOrEmpty(smtpHost)) services.AddSingleton<IEmailTransport, SmtpEmailTransport>();
        else if (env.IsDevelopment()) services.AddSingleton<IEmailTransport, LogEmailTransport>();
        else throw new InvalidOperationException("Smtp:Host must be configured outside Development.");

        services.AddSingleton(Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(EmailQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        }));
        services.AddSingleton<IEmailSender, QueuedEmailSender>();
        services.AddHostedService<EmailDispatchWorker>();

        return services;
    }

    /// <summary>Applies pending EF migrations (Development, tests, or single-instance deployments that opt in).</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<KopeckDb>().Database.MigrateAsync();
    }
}
