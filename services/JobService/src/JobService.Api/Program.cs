using JobService.Api.Middleware;
using JobService.Application;
using JobService.Infrastructure;
using JobService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Extracts TenantId from JWT/API key and makes it available via ITenantContext
app.UseMiddleware<TenantContextMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

// Auto-apply migrations on startup (dev only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var conn = config.GetConnectionString("Jobs");
    if (!string.IsNullOrWhiteSpace(conn))
    {
        await scope.ServiceProvider
            .GetRequiredService<JobsDbContext>()
            .Database.MigrateAsync();
    }
}

await app.RunAsync();