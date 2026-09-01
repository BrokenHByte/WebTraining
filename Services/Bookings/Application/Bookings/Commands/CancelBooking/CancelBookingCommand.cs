using MediatR;

namespace Bookings.Application.Bookings.Commands.DeleteBooking;

public sealed record CancelBookingCommand : IRequest
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string UserRole { get; init; }   
}