using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Repositories;

namespace WebProject.Services;

public interface IBookingService
{
    Task<Booking> CreateAsync(Guid eventId);
    Task<Booking> GetByIdAsync(Guid bookingId);
    IQueryable<Booking> GetAll();
    IQueryable<Booking> GetPending();
    IQueryable<Booking> GetBookingsByEventAsync(Guid eventId);
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

    public IQueryable<Booking> GetPending()
    {
        return bookingRepository.GetPending();
    }

    public IQueryable<Booking> GetBookingsByEventAsync(Guid eventId)
    {
        return bookingRepository.GetBookingsByEventAsync(eventId);
    }

    public async Task Update(Guid bookingId, Booking data)
    {
        await bookingRepository.Update(bookingId, data);
        var bookingEntity = await bookingRepository.GetByIdAsync(bookingId);
        // По идее мжно напрямую менять bookingEntity, но это сразу погружение в контекст.
        // По этому через копию
        await bookingRepository.Update(bookingId, new Booking
        {
            Id = bookingEntity.Id,
            EventId = bookingEntity.EventId,
            Status = data.Status,
            CreatedAt = bookingEntity.CreatedAt,
            ProcessedAt = data.ProcessedAt
        });
    }

    public async Task DeleteById(Guid bookingId)
    {
        var bookingEntity = await bookingRepository.GetByIdAsync(bookingId);
        var eventId = bookingEntity.EventId;
        await bookingRepository.DeleteById(bookingId);

        await BookingSemaphore.WaitAsync();

        try
        {
            var eventOne = await eventService.GetEventByIdAsync(eventId);
            eventOne.ReleaseSeats();
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<Booking> CreateAsync(Guid eventId)
    {
        await BookingSemaphore.WaitAsync();

        try
        {
            var eventOne = await eventService.GetEventByIdAsync(eventId);

            if (!eventOne.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
        }
        finally
        {
            BookingSemaphore.Release();
        }

        return await bookingRepository.CreateAsync(eventId);
    }

    public async Task<Booking> GetByIdAsync(Guid bookingId)
    {
        return await bookingRepository.GetByIdAsync(bookingId);
    }

    public IQueryable<Booking> GetAll()
    {
        return bookingRepository.GetAll();
    }
}