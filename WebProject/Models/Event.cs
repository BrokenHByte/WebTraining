using System.ComponentModel.DataAnnotations.Schema;

namespace WebProject.Models;

public class Event
{
    public Event()
    {
        Title = null!;
    }

    public Guid Id { get; init; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public List<Booking> Bookings { get; set; }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            return false;
        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
    }
}