using MediatR;

namespace Events.Application.Events.Commands.ReserveSeat;

public class ReserveSeatCommand : IRequest
{
    public required string BookingId { get; init; }
    public required string EventId { get; init; }
    public required string UserId { get; init; }
}