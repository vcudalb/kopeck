using Kopeck.Api.Http;
using Kopeck.Application;
using Kopeck.Infrastructure;

// Composition root: each layer registers its own services; this file only wires them together.
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddWebApi(builder.Configuration);

var app = builder.Build();

// Schema comes from EF migrations. Auto-apply in Development/tests; in production run `dotnet ef database update`
// as a deployment step, or set Database:MigrateOnStartup=true for single-instance setups.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

app.UseWebApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapEndpointModules();

await app.RunAsync();

public partial class Program;
