

namespace Contracts.Messages;

[Serializable]
public record CreateBookingMessage
{
    public required string BookingId { get; init; }
    public required string EventId { get; init; }
    public required string UserId { get; init; }
}