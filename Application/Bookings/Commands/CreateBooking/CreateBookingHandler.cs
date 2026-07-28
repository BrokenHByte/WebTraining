using Application.Abstractions.Persistence.Repositories;
using Application.Events.Commands.CreateEvent;
using MediatR;

namespace Application.Bookings.Commands.CreateBooking;

public class CreateBookingHandler(IEventRepository eventRepository, IBookingRepository bookingRepository) : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.CreateAsync(request.EventId);
        return new CreateBookingResponse { Id = booking.Id, EventId = request.EventId, Status = booking.Status };
    }
}