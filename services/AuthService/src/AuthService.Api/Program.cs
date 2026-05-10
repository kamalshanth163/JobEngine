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

// Auto-apply migrations on startup (dev only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider
        .GetRequiredService<AuthDbContext>()
        .Database.MigrateAsync();
}

await app.RunAsync();