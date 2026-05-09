using JobService.Application.Common.Interfaces;
using JobService.Infrastructure.Persistence;
using JobService.Infrastructure.Persistence.Repositories;
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
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(configuration["Redis__Connection"]!));

        // MassTransit — connects to RabbitMQ for publishing events
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ__Host"], h =>
                {
                    h.Username(configuration["RabbitMQ__Username"] ?? "guest");
                    h.Password(configuration["RabbitMQ__Password"] ?? "guest");
                });
            });
        });

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<JobsDbContext>());
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        return services;
    }
}