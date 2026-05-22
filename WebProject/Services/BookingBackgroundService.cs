using WebProject.Models;

namespace WebProject.Services;

public class BookingBackgroundService(IBookingService bookingService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var bookings = bookingService.GetBookings().Where(x => x.Status == Booking.BookingStatus.Pending).ToList();
            foreach (var booking in bookings)
            {
                await Task.Delay(2000, stoppingToken);
                //var updatedBooking = booking with { Status = Booking.BookingStatus.Confirmed };
                bookingService.UpdateBooking(booking.Id, booking);
            }
        }
    }
}