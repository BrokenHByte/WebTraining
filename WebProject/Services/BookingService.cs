using System.Collections.Concurrent;
using WebProject.Exceptions;
using WebProject.Models;

namespace WebProject.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(Guid eventId);
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
    IEnumerable<Booking> GetBookings();
    IEnumerable<Booking> GetPending();
    void UpdateBooking(Guid bookingId, Booking data);
    void DeleteBookingById(Guid bookingId);
}

public class BookingService(IEventService eventService, ILogger<BookingService> logger) : IBookingService
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    private readonly SemaphoreSlim _bookingSemaphore = new(1, 1);

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var guid = Guid.NewGuid();
        await _bookingSemaphore.WaitAsync();

        try
        {
            var eventOne = await eventService.GetEventByIdAsync(eventId);
            if (eventOne == null)
                throw new EventNotFoundException("Event not found");

            if (!eventOne.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
        }
        finally
        {
            _bookingSemaphore.Release();
        }

        _bookings.TryAdd(guid, new Booking
        {
            Id = guid,
            EventId = eventId,
            Status = Booking.BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        });

        return _bookings[guid];
    }

    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        if (!_bookings.TryGetValue(bookingId, out var bookingById))
        {
            logger.LogError("Booking not found");
            throw new BookingNotFoundException("Booking not found");
        }

        return Task.FromResult(bookingById);
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

    public IEnumerable<Booking> GetPending()
    {
        return _bookings.Select(x => x.Value).Where(x => x.Status == Booking.BookingStatus.Pending);
    }

    public async void DeleteBookingById(Guid bookingId)
    {
        var guidEvent = Guid.Empty;
        if (_bookings.TryGetValue(bookingId, out var eventToDelete))
            guidEvent = eventToDelete.EventId;

        if (!_bookings.TryRemove(bookingId, out _))
        {
            logger.LogError($"Booking with id {bookingId} not found");
            throw new BookingNotFoundException($"Booking {bookingId} not found");
        }

        await _bookingSemaphore.WaitAsync();

        try
        {
            var eventOne = await eventService.GetEventByIdAsync(guidEvent);
            eventOne.ReleaseSeats();
        }
        finally
        {
            _bookingSemaphore.Release();
        }
    }
}