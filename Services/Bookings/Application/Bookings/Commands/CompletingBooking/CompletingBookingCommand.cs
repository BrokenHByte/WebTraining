using MediatR;

namespace Bookings.Application.Bookings.Commands.CompletingBooking;

public sealed record CompletingBookingCommand : IRequest
{
    public Guid BookingId { get; init; }
}