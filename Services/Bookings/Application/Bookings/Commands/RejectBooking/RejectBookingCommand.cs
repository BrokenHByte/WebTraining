using MediatR;

namespace Bookings.Application.Bookings.Commands.RejectBooking;

public sealed record RejectBookingCommand : IRequest
{
    public Guid BookingId { get; init; }
    public required string Error { get; init; }
}