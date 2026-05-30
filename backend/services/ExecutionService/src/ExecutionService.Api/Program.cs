using ExecutionService.Core.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Register all job handlers — each implements IJobHandler
// JobHandlerRegistry resolves them by JobType string
builder.Services.AddScoped<IJobHandler, SendEmailHandler>();
builder.Services.AddScoped<IJobHandler, GenerateReportHandler>();
builder.Services.AddScoped<IJobHandler, DataSyncHandler>();
builder.Services.AddScoped<JobHandlerRegistry>();

var app = builder.Build();
app.MapControllers();
app.MapHealthChecks("/health");
await app.RunAsync();