using Application.Abstractions.Persistence.Repositories;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Repositories;

public class BookingRepository(ILogger<BookingRepository> logger, AppDbContext db) : IBookingRepository
{
    public IQueryable<Booking> GetBookingsByUser(Guid userId)
    {
        return db.Bookings.Where(x => x.UserId == userId);
    }

    public async Task<Booking> CreateAsync(Guid eventId, Guid userId)
    {
        var guid = Guid.NewGuid();
        var booking = await db.Bookings.AddAsync(new Booking
        {
            Id = guid,
            EventId = eventId,
            Status = Booking.BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null,
            UserId = userId
        });

        await db.SaveChangesAsync();
        return booking.Entity;
    }

    public async Task<Booking> GetByIdAsync(Guid bookingId)
    {
        var booking = await db.Bookings.FindAsync(bookingId);

        if (booking == null)
        {
            logger.LogError("Booking not found");
            throw new BookingNotFoundException("Booking not found");
        }

        return booking;
    }

    public IQueryable<Booking> GetAll()
    {
        return db.Bookings;
    }

    public IQueryable<Booking> GetPending()
    {
        return db.Bookings.Where(x => x.Status == Booking.BookingStatus.Pending);
    }

    public async Task UpdateAsync(Guid bookingId, Booking data)
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

    public async Task DeleteByIdAsync(Guid bookingId)
    {
        var oneBooking = await db.Bookings.Where(x => x.Id == bookingId).FirstOrDefaultAsync();

        if (oneBooking == null)
        {
            logger.LogError($"Booking with id {bookingId} not found");
            throw new BookingNotFoundException($"Booking {bookingId} not found");
        }

        db.Bookings.Remove(oneBooking);
        await db.SaveChangesAsync();
    }

    public async Task CancelledByIdAsync(Guid bookingId)
    {
        var oneBooking = await db.Bookings.Where(x => x.Id == bookingId && x.Status != Booking.BookingStatus.Cancelled).FirstOrDefaultAsync();

        if (oneBooking == null)
        {
            logger.LogError($"Booking with id {bookingId} not found or booking cancelled");
            throw new BookingNotFoundException($"Booking {bookingId} not found or booking cancelled");
        }

        oneBooking.Status = Booking.BookingStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public IQueryable<Booking> GetBookingsByEvent(Guid eventId)
    {
        return db.Bookings.Where(x => x.EventId == eventId);
    }
}