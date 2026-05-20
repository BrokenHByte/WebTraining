namespace WebProject.Models;

public class Booking
{
    public enum BookingStatus {Pending, Confirmed, Rejected}
    
    public Guid Id { get; set; }
    public Guid EventId  { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}