namespace WebProject.Models;

public class Booking : ICloneable
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Rejected
    }

    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }

    public Booking Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
        return this;
    }

    public Booking Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
        return this;
    }
}