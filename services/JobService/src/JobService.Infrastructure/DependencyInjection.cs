using JobService.Application.Common.Interfaces;
using JobService.Infrastructure.Messaging;
using JobService.Infrastructure.Persistence;
using JobService.Infrastructure.Persistence.Repositories;
using JobService.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace JobService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core — connects to je_jobs database
        services.AddDbContext<JobsDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Jobs"),
                b => b.MigrationsAssembly(typeof(JobsDbContext).Assembly.FullName)));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConnection = configuration["Redis__Connection"];
            if (string.IsNullOrWhiteSpace(redisConnection))
                throw new InvalidOperationException("Missing Redis__Connection configuration");

            var redisOptions = ConfigurationOptions.Parse(redisConnection);
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectRetry = 5;
            redisOptions.ConnectTimeout = 5000;

            return ConnectionMultiplexer.Connect(redisOptions);
        });

        // MassTransit — connects to RabbitMQ for publishing events
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                var rabbitHost = configuration["RabbitMQ__Host"] ?? "rabbitmq";
                var rabbitUsername = configuration["RabbitMQ__Username"] ?? "guest";
                var rabbitPassword = configuration["RabbitMQ__Password"] ?? "guest";

                cfg.Host(rabbitHost, h =>
                {
                    h.Username(rabbitUsername);
                    h.Password(rabbitPassword);
                });
            });
        });

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        // Event publisher wrapper for MassTransit IPublishEndpoint
        services.AddScoped<IEventPublisher>(sp =>
            new MassTransitEventPublisher(sp.GetRequiredService<IPublishEndpoint>()));

        // Tenant quota enforcement service
        services.AddScoped<ITenantQuotaService, TenantQuotaService>();

        // Note: HttpTenantContext and HttpContextAccessor are provided by the Api project.
        return services;
    }
}