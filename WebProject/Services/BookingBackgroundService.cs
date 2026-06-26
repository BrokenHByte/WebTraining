using WebProject.DataAccess;
using WebProject.Exceptions;
using WebProject.Models;

namespace WebProject.Services;

public class BookingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<BookingBackgroundService> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            List<Booking> pendingBookings;
            using (var scope = scopeFactory.CreateScope())
            {
                var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                pendingBookings = bookingService.GetPending().ToList();
                var tasks = pendingBookings.Select(booking =>
                    ProcessBookingAsync(eventService, bookingService, booking, stoppingToken));
                await Task.WhenAll(tasks);
                if (pendingBookings.Count > 0)
                    logger.LogInformation($"Booking {pendingBookings.Count} bookings updated.");
            }

            await Task.Delay(100, stoppingToken);
        }


        logger.LogInformation("Booking background service stopped");
    }

    private async Task ProcessBookingAsync(IEventService eventService, IBookingService bookingService, Booking booking,
        CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);
        Event? existedEvent = null;
        var cloneBooking = booking;

        if (stoppingToken.IsCancellationRequested)
            return;

        try
        {
            await _processingSemaphore.WaitAsync(stoppingToken);
            existedEvent = await eventService.GetEventByIdAsync(cloneBooking.EventId); 
            bookingService.UpdateBooking(booking.Id, cloneBooking.Confirm());
        }
        catch (EventNotFoundException ex)
        {
            bookingService.UpdateBooking(booking.Id, cloneBooking.Reject());
            logger.LogWarning($"Booking {cloneBooking.EventId} rejected. Event not found");
        }
        catch (OperationCanceledException ex)
        {
            // Остановка сервиса. Вероятно штатная ситуация
        }
        catch (Exception ex)
        {
            bookingService.UpdateBooking(booking.Id, cloneBooking.Reject());
            if (existedEvent != null) existedEvent.ReleaseSeats();
            logger.LogWarning($"Booking {cloneBooking.EventId} rejected. ");
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}