namespace Contracts.Messages;

[Serializable]
public class ConfirmationBookingMessage
{
    public required string BookingId { get; init; }
}