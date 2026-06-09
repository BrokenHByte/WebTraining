using WebProject.Exceptions;
using WebProject.Models;

namespace WebProject.Services;

public class BookingBackgroundService(
    IBookingService bookingService,
    IEventService eventService,
    ILogger<BookingBackgroundService> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingBookings = bookingService.GetPending().ToList();
            var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
            await Task.WhenAll(tasks); // TODO: лучше ограничить кол-во потоков
            if (pendingBookings.Count > 0) logger.LogInformation($"Booking {pendingBookings.Count} bookings updated.");

            await Task.Delay(100, stoppingToken);
        }

        logger.LogInformation("Booking background service stopped");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);
        Event? existedEvent = null;
        var cloneBooking = booking;

        if (stoppingToken.IsCancellationRequested)
            return;

        try
        {
            await _processingSemaphore.WaitAsync(stoppingToken);
            // existedEvent может либо вернуться, либо exception. Null не может быть
            existedEvent = await eventService.GetEventByIdAsync(cloneBooking.EventId); // TODO: stoppingToken
            bookingService.UpdateBooking(booking.Id, cloneBooking.Confirm()); // TODO: await, stoppingToken
        }
        catch (EventNotFoundException ex)
        {
            bookingService.UpdateBooking(booking.Id, cloneBooking.Reject()); // TODO: await, stoppingToken
            logger.LogWarning($"Booking {cloneBooking.EventId} rejected. Event not found");
        }
        catch (OperationCanceledException ex)
        {
        }
        catch (Exception ex)
        {
            bookingService.UpdateBooking(booking.Id, cloneBooking.Reject()); // TODO: await, stoppingToken
            if (existedEvent != null) existedEvent.ReleaseSeats();
            logger.LogWarning($"Booking {cloneBooking.EventId} rejected. ");
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}