namespace Contracts.Messages;

[Serializable]
public class RejectBookingMessage
{
    public required string BookingId { get; init; }
    public required string Error { get; init; }
    public required string CodeError { get; init; }
}