using AuthService.Application;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();      // MediatR, FluentValidation
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.MapControllers();
app.MapHealthChecks("/health");

// Auto-apply migrations on startup in all environments.
// In containers, Postgres can come up slightly later, so retry briefly.
for (var attempt = 1; attempt <= 10; attempt++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthService.Startup");

        var conn = config.GetConnectionString("Auth");
        if (!string.IsNullOrWhiteSpace(conn))
        {
            await scope.ServiceProvider
                .GetRequiredService<AuthDbContext>()
                .Database.MigrateAsync();
        }

        break;
    }
    catch (Exception ex) when (attempt < 10)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthService.Startup");
        logger.LogWarning(ex,
            "Auth DB migration attempt {Attempt}/10 failed. Retrying in 3s...",
            attempt);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}

await app.RunAsync();