using MediatR;

namespace Application.Bookings.Commands.CreateBooking;

public sealed record CreateBookingCommand : IRequest<CreateBookingResponse>
{
    public Guid EventId { get; init; }    
}