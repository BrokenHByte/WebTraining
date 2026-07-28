using Domain.Entities;

namespace Application.Bookings.Commands.CreateBooking;

public sealed record CreateBookingResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Booking.BookingStatus Status { get; set; }
}