namespace WebProject.Models;

public class Event
{
    public int AvailableSeats;
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public int TotalSeats { get; init; }

    public bool TryReserveSeats(int count = 1)
    {
        int copySeats;

        do
        {
            copySeats = AvailableSeats;
            if (copySeats < count)
                return false;
        } while (Interlocked.CompareExchange(ref AvailableSeats, copySeats - count, copySeats) != copySeats);

        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        Interlocked.Add(ref AvailableSeats, count);
    }
}