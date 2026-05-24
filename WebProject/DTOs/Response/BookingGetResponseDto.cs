using WebProject.Models;

namespace WebProject.DTOs.Response;

public class BookingGetResponseDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Booking.BookingStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}