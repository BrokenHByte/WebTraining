using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Repositories;

namespace WebProject.Services;

public interface IBookingService
{
    Task<Booking> CreateAsync(Guid eventId);
    Task<Booking> GetByIdAsync(Guid bookingId);
    IEnumerable<Booking> GetAll();
    IEnumerable<Booking> GetPending();
    Task Update(Guid bookingId, Booking data);
    Task DeleteById(Guid bookingId);
}

public class BookingService(
    IBookingRepository bookingRepository,
    IEventService eventService,
    ILogger<BookingService> logger)
    : IBookingService
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    public IEnumerable<Booking> GetPending()
    {
        return db.Bookings.Where(x => x.Status == Booking.BookingStatus.Pending);
    }

    public async Task UpdateBooking(Guid bookingId, Booking data)
    {
        var bookingEntity = await db.Bookings.FindAsync(bookingId);

        if (bookingEntity != null)
        {
            bookingEntity.Status = data.Status;
            bookingEntity.ProcessedAt = data.ProcessedAt;
            await db.SaveChangesAsync();
            return;
        }

        logger.LogError($"Booking with id {bookingId} not found");
        throw new BookingNotFoundException($"Booking {bookingId} not found");
    }

    public async Task DeleteBookingById(Guid bookingId)
    {
        var oneBooking = await db.Bookings.Where(x => x.Id == bookingId).FirstOrDefaultAsync();

        if (oneBooking == null)
        {
            logger.LogError($"Booking with id {bookingId} not found");
            throw new BookingNotFoundException($"Booking {bookingId} not found");
        }

        var guidEvent = oneBooking.EventId;
        db.Bookings.Remove(oneBooking);
        await db.SaveChangesAsync();
        await BookingSemaphore.WaitAsync();

        try
        {
            var eventOne = await eventService.GetEventByIdAsync(guidEvent);
            eventOne.ReleaseSeats();
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var guid = Guid.NewGuid();
        await BookingSemaphore.WaitAsync();

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
            BookingSemaphore.Release();
        }

        return bookingRepository.CreateAsync(eventId);
    }

    public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await db.Bookings.FindAsync(bookingId);

        if (booking == null)
        {
            logger.LogError("Booking not found");
            throw new BookingNotFoundException("Booking not found");
        }

        return booking;
    }

    public IEnumerable<Booking> GetBookings()
    {
        return db.Bookings.ToList();
    }
}