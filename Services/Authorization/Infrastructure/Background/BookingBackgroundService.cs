using Application.Bookings.Commands.CompletingBooking;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Background;

public class TestCompletingBookingBackgroundService(IServiceProvider provider,
    ILogger<TestCompletingBookingBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        while (!stoppingToken.IsCancellationRequested)
        {
            await mediator.Send(new CompletingBookingCommand());
            await Task.Delay(100, stoppingToken);
        }
        logger.LogInformation("Booking background service stopped");
    }
}