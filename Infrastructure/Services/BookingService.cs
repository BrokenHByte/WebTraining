using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Presentation.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public interface IBookingService
{
    Task<Booking> GetByIdAsync(Guid bookingId);
    IQueryable<Booking> GetAll();
    IQueryable<Booking> GetPending();
    IQueryable<Booking> GetBookingsByEvent(Guid eventId);

    Task<Booking> CreateAsync(Guid eventId);
    Task UpdateAsync(Guid bookingId, Booking data);
    Task DeleteByIdAsync(Guid bookingId);
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

    public IQueryable<Booking> GetBookingsByEvent(Guid eventId)
    {
        return bookingRepository.GetBookingsByEvent(eventId);
    }

    public async Task UpdateAsync(Guid bookingId, Booking data)
    {
        await bookingRepository.UpdateAsync(bookingId, data);
        var bookingEntity = await bookingRepository.GetByIdAsync(bookingId);
        await bookingRepository.UpdateAsync(bookingId, new Booking
        {
            Id = bookingEntity.Id,
            EventId = bookingEntity.EventId,
            Status = data.Status,
            CreatedAt = bookingEntity.CreatedAt,
            ProcessedAt = data.ProcessedAt
        });
    }

    public async Task DeleteByIdAsync(Guid bookingId)
    {
        var bookingEntity = await bookingRepository.GetByIdAsync(bookingId);
        var eventId = bookingEntity.EventId;
        await bookingRepository.DeleteByIdAsync(bookingId);

        await BookingSemaphore.WaitAsync();

        try
        {
            var eventOne = await eventService.GetByIdAsync(eventId);
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
            var eventOne = await eventService.GetByIdAsync(eventId);

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