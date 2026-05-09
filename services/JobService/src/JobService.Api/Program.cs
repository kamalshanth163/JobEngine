using JobService.Api.Middleware;
using JobService.Application;
using JobService.Infrastructure;
using JobService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Jobs")!)
    .AddRedis(builder.Configuration["Redis__Connection"]!)
    .AddRabbitMQ();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Extracts TenantId from JWT/API key and makes it available via ITenantContext
app.UseMiddleware<TenantContextMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider
        .GetRequiredService<JobsDbContext>()
        .Database.MigrateAsync();
}

await app.RunAsync();