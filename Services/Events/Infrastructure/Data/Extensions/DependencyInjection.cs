using Contracts.Kafka;
using Contracts.Messages;
using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using Events.Infrastructure.Cache;
using Events.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Events.Infrastructure.Data.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("EventsConnection")).UseSnakeCaseNamingConvention());
        services.AddScoped<IEventRepository, EventRepository>();

        services.Configure<RedisConfig>(configuration.GetSection("Redis"));
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<RedisConfig>>().Value;
            var options = ConfigurationOptions.Parse(config.ConnectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 1;
            options.ConnectTimeout = 100;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }


    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}