namespace WebProject.Models;

public record Booking
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Rejected
    }

    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public BookingStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
}