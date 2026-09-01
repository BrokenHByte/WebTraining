namespace Contracts.Messages;

[Serializable]
public class CancelledBookingMessage
{
    public required string EventId { get; init; }
}