using Domain.Entities;

namespace Application.Bookings.DTOs;

public class BookingCreateResponseDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Booking.BookingStatus Status { get; set; }
}