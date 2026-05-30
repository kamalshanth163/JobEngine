using AuthService.Application.Common.Interfaces;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Microsoft.Extensions.Options;

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
        // Attempt to connect if configuration present; don't throw on invalid config.
        // We create the JwtTokenService with the optional multiplexer below.

        // Repository implementations
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<AuthDbContext>());

        // Services
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Register JwtTokenService using a factory so Redis connection errors don't crash startup
        services.AddSingleton<IJwtTokenService>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<JwtOptions>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConn = config["Redis__Connection"] ?? config["Redis:Connection"];
            IConnectionMultiplexer? mux = null;
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                try
                {
                    mux = ConnectionMultiplexer.Connect(redisConn);
                }
                catch
                {
                    // swallow and continue without Redis cache
                    mux = null;
                }
            }
            return new JwtTokenService(opts, mux);
        });

        // JWT options from appsettings.json
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        return services;
    }
}