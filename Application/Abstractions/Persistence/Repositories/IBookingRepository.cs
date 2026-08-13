using Domain.Entities;

namespace Application.Abstractions.Persistence.Repositories;

public interface IBookingRepository
{
    Task<Booking> GetByIdAsync(Guid bookingId);
    IQueryable<Booking> GetAll();
    IQueryable<Booking> GetPending();
    IQueryable<Booking> GetBookingsByEvent(Guid eventId);
    IQueryable<Booking> GetBookingsByUser(Guid userId);   

    Task<Booking> CreateAsync(Guid eventId, Guid userId);
    Task UpdateAsync(Guid bookingId, Booking data);
    Task DeleteByIdAsync(Guid bookingId);
    Task CancelledByIdAsync(Guid bookingId);
}