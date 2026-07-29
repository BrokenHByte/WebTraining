using Domain.Entities;

namespace Application.Abstractions.Persistence.Services;

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
