using Kopeck.Application.Abstractions;
using Kopeck.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kopeck.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public CapturingEmail Mail { get; } = new();
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder b)
    {
        _conn.Open();
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimit:AuthPerMinute"] = "10000" }));
        b.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<KopeckDb>>();
            s.AddDbContext<KopeckDb>(o => o.UseSqlite(_conn));
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(Mail);
        });
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); _conn.Dispose(); }
}
