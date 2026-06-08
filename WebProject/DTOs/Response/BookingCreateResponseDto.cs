using WebProject.Models;

namespace WebProject.DTOs.Response;

public class BookingCreateResponseDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Booking.BookingStatus Status { get; set; }
}