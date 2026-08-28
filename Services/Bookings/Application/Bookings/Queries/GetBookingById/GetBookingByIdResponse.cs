using Domain.Entities;

namespace Application.Bookings.Queries.GetBookingById;

public sealed record GetBookingByIdResponse
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Booking.BookingStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}