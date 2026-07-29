using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using Application.Events.Commands.CreateEvent;
using MediatR;

namespace Application.Bookings.Commands.CreateBooking;

public class CreateBookingHandler(IBookingService bookingService) : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.CreateAsync(request.EventId);
        return new CreateBookingResponse { Id = booking.Id, EventId = request.EventId, Status = booking.Status };
    }
}