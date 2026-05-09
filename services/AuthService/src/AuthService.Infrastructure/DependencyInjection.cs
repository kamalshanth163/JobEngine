using AuthService.Application.Common.Interfaces;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core — connects to je_auth database
        services.AddDbContext<AuthDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Auth"),
                b => b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName)));

        // Redis — token validation cache
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(configuration["Redis__Connection"]!));

        // Repository implementations
        services.AddScoped<ITenantRepository,  TenantRepository>();
        services.AddScoped<IUserRepository,    UserRepository>();
        services.AddScoped<IApiKeyRepository,  ApiKeyRepository>();
        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<AuthDbContext>());

        // Services
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher,  BCryptPasswordHasher>();

        // JWT options from appsettings.json
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        return services;
    }
}