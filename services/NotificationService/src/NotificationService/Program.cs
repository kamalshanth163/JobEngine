using MassTransit;
using NotificationService.Consumers;
using NotificationService.Webhooks;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<WebhookDeliveryService>();
builder.Services.AddHttpClient();

// MassTransit — subscribe to job events from RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<JobCompletedConsumer>();
    x.AddConsumer<JobFailedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ__Host"], h =>
        {
            h.Username("guest"); h.Password("guest");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
await app.RunAsync();