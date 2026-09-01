using MediatR;

namespace Bookings.Application.Bookings.Commands.CreateBooking;

public sealed record CreateBookingCommand : IRequest<CreateBookingResponse>
{
    public required string EventId { get; init; }
    public required string UserId { get; init; }
    public required string UserRole { get; init; }   
}