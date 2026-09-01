using Bookings.Application.Abstractions.Persistence.Repositories;
using Bookings.Infrastructure.Data.Repositories;
using Contracts.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bookings.Infrastructure.Data.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingsConnection")).UseSnakeCaseNamingConvention());
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddSingleton<KafkaProducerService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<KafkaProducerService>>();
            var server = configuration["Kafka:BootstrapServers"];
            var cliendId = "booking-service";
            return new KafkaProducerService(server, cliendId, logger);
        }); ;

        return services;
    }


    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}