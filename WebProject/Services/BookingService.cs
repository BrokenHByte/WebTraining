using System.Collections.Concurrent;
using WebProject.Models;

namespace WebProject.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
}

public class BookingService(IEventService eventService) : IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    private readonly IEventService _eventService = eventService;

    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        _eventService.GetEventById(eventId)

        var guid = Guid.NewGuid();
        _bookings.TryAdd(guid, new Booking
        {
            Id = guid,
            EventId = eventId,
            Status = Booking.BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        });
        return Task.FromResult(_bookings[guid]);
    }

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        return Task.FromResult(_bookings.GetValueOrDefault(bookingId));
    }
}