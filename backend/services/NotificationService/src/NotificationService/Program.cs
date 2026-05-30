using MassTransit;
using NotificationService.Consumers;
using NotificationService.Webhooks;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<WebhookDeliveryService>();
builder.Services.AddSingleton<IWebhookRepository, ConfigurationWebhookRepository>();
builder.Services.AddHttpClient();
builder.Services.Configure<WebhookOptions>(builder.Configuration.GetSection(WebhookOptions.SectionName));

// MassTransit — subscribe to job events from RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<JobCompletedConsumer>();
    x.AddConsumer<JobFailedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ__Host"] ?? "rabbitmq";
        var username = builder.Configuration["RabbitMQ__Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ__Password"] ?? "guest";

        cfg.Host(host, h =>
        {
            h.Username(username);
            h.Password(password);
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
await app.RunAsync();