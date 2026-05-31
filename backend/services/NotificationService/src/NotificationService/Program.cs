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
        var host = GetValueOrDefault(builder.Configuration, "RabbitMQ__Host", "RabbitMQ:Host", "rabbitmq");
        var username = GetValueOrDefault(builder.Configuration, "RabbitMQ__Username", "RabbitMQ:Username", "guest");
        var password = GetValueOrDefault(builder.Configuration, "RabbitMQ__Password", "RabbitMQ:Password", "guest");

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

static string GetValueOrDefault(IConfiguration configuration, string primaryKey, string fallbackKey, string defaultValue)
{
    var primary = configuration[primaryKey];
    if (!string.IsNullOrWhiteSpace(primary))
    {
        return primary;
    }

    var fallback = configuration[fallbackKey];
    if (!string.IsNullOrWhiteSpace(fallback))
    {
        return fallback;
    }

    return defaultValue;
}