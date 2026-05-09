using MassTransit;
using StackExchange.Redis;
using WorkerService.Clients;
using WorkerService.Consumers;
using WorkerService.Locking;

var builder = Host.CreateApplicationBuilder(args);

// Redis — single multiplexer shared across all consumers
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis__Connection"]!));

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
        cfg.Host(builder.Configuration["RabbitMQ__Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ__Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ__Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("job-submitted", e =>
        {
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
    c.BaseAddress = new Uri(builder.Configuration["ExecutionService__Url"]!));

builder.Services.AddJobEngineObservability("worker-service");
builder.Services.AddHealthChecks()
    .AddRabbitMQ()
    .AddRedis(builder.Configuration["Redis__Connection"]!);

var app = builder.Build();
await app.RunAsync();