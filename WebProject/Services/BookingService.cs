using System.Collections.Concurrent;
using WebProject.Exceptions;
using WebProject.Models;

namespace WebProject.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId);
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    IEnumerable<Booking> GetBookings();
    void UpdateBooking(Guid bookingId, Booking data);
    void DeleteBookingById(Guid bookingId);
}

public class BookingService(IEventService eventService, ILogger<BookingService> logger) : IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();


    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        if (!eventService.ContainsById(eventId))
            throw new EventNotFoundException($"Event with id {eventId} does not exist");

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

    public IEnumerable<Booking> GetBookings()
    {
        return _bookings.Select(x => x.Value);
    }

    public void UpdateBooking(Guid bookingId, Booking data)
    {
        if (!_bookings.TryGetValue(bookingId, out var existingBooking))
        {
            logger.LogError($"Booking with id {bookingId} not found");
            throw new BookingNotFoundException($"Booking {bookingId} not found");
        }

        var updatedBooking = new Booking
        {
            Id = existingBooking.Id,
            EventId = existingBooking.EventId,
            Status = data.Status,
            CreatedAt = existingBooking.CreatedAt,
            ProcessedAt = data.ProcessedAt
        };

        if (!_bookings.TryUpdate(bookingId, updatedBooking, existingBooking))
        {
            logger.LogError($"Booking with id {bookingId} not found");
            throw new BookingNotFoundException($"Booking {bookingId} not found");
        }
    }

    public void DeleteBookingById(Guid bookingId)
    {
        if (!_bookings.TryRemove(bookingId, out _))
        {
            logger.LogError($"Booking with id {bookingId} not found");
            throw new BookingNotFoundException($"Booking {bookingId} not found");
        }
    }
}