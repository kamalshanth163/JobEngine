using MassTransit;
using JobEngine.Shared.Contracts.Events;
using JobService.Application.Common.Interfaces;
using JobService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WorkerService.Clients;
using WorkerService.Consumers;
using WorkerService.Locking;
using WorkerService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ITenantContext, WorkerTenantContext>();

builder.Services.AddDbContext<JobsDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Jobs")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Jobs")));

builder.Services.AddScoped<IJobStatusUpdater, JobStatusUpdater>();
builder.Services.AddSingleton<IWorkerIdentity, WorkerIdentity>();
builder.Services.AddHostedService<HeartbeatService>();

var redisConnection = GetRequiredSetting(builder.Configuration, "Redis:Connection", "Redis__Connection");
var executionServiceUrl = GetRequiredSetting(builder.Configuration, "ExecutionService:Url", "ExecutionService__Url");

// Redis — single multiplexer shared across all consumers
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

// MassTransit — connects to RabbitMQ, registers consumers
// Polly retry: 3 attempts, 2s/4s/8s exponential backoff
// Dead letter: jobs that exhaust retries go to job-submitted_error queue
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<JobSubmittedConsumer>()
     .Endpoint(e => e.Name = "job-submitted");

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rabbitHost = GetRequiredSetting(builder.Configuration, "RabbitMQ__Host", "RabbitMQ:Host");
        var rabbitUsername = GetRequiredSetting(builder.Configuration, "RabbitMQ__Username", "RabbitMQ:Username");
        var rabbitPassword = GetRequiredSetting(builder.Configuration, "RabbitMQ__Password", "RabbitMQ:Password");
        var rabbitVirtualHost = builder.Configuration["RabbitMQ__VirtualHost"]
            ?? builder.Configuration["RabbitMQ:VirtualHost"]
            ?? "/";

        cfg.Host(rabbitHost, rabbitVirtualHost, h =>
        {
            h.Username(rabbitUsername);
            h.Password(rabbitPassword);
        });

        cfg.ReceiveEndpoint("job-submitted", e =>
        {
            // Explicitly bind message exchange to this queue for cross-service publish/consume.
            e.Bind<JobSubmittedEvent>();

            // Manual ACK — message stays in queue until we explicitly ACK
            // If worker crashes, RabbitMQ redelivers to another worker
            e.PrefetchCount = 5;

            // Polly resilience pipeline for transient failures
            e.UseMessageRetry(r => r
                .Exponential(3,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(2)));

            // Dead letter exchange — exhausted jobs land here
            e.BindDeadLetterQueue("job-submitted_dlx",
                "job-submitted_error");

            e.ConfigureConsumer<JobSubmittedConsumer>(ctx);
        });
    });
});

// Distributed lock manager
builder.Services.AddSingleton<IDistributedLockManager, RedisLockManager>();
builder.Services.AddHttpClient<IExecutionServiceClient, ExecutionServiceClient>(c =>
    c.BaseAddress = new Uri(executionServiceUrl));

var app = builder.Build();
await app.RunAsync();

static string GetRequiredSetting(
    IConfiguration configuration,
    params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
            return value;
    }

    throw new InvalidOperationException(
        $"Missing configuration value. Checked keys: {string.Join(", ", keys)}");
}