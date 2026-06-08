using WebProject.Models;

namespace WebProject.Services;

public class BookingBackgroundService(IBookingService bookingService, ILogger<BookingBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var bookings = bookingService.GetBookings().Where(x => x.Status == Booking.BookingStatus.Pending).ToList();
            foreach (var booking in bookings)
            {
                await Task.Delay(2000, stoppingToken);
                var updatedBooking = booking with
                {
                    Status = Booking.BookingStatus.Confirmed, ProcessedAt = DateTime.UtcNow
                };
                bookingService.UpdateBooking(booking.Id, updatedBooking);
            }

            if (bookings.Count > 0) logger.LogInformation($"Booking {bookings.Count} bookings updated.");

            await Task.Delay(100, stoppingToken);
        }

        logger.LogInformation("Booking background service stopped");
    }
}