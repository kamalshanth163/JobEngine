using JobService.Api.Middleware;
using JobService.Application;
using JobService.Application.Common.Interfaces;
using JobService.Infrastructure;
using JobService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret configuration is missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer configuration is missing.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience configuration is missing.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseAuthentication();

// Extracts TenantId from JWT/API key and makes it available via ITenantContext
app.UseMiddleware<TenantContextMiddleware>();

app.UseAuthorization();

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
            .CreateLogger("JobService.Startup");

        var conn = config.GetConnectionString("Jobs");
        if (!string.IsNullOrWhiteSpace(conn))
        {
            await scope.ServiceProvider
                .GetRequiredService<JobsDbContext>()
                .Database.MigrateAsync();
        }

        break;
    }
    catch (Exception ex) when (attempt < 10)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("JobService.Startup");
        logger.LogWarning(ex,
            "Jobs DB migration attempt {Attempt}/10 failed. Retrying in 3s...",
            attempt);
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}

await app.RunAsync();