using Application.Abstractions.Persistence.Repositories;
using Domain.Exceptions;
using MediatR;

namespace Application.Bookings.Commands.CreateBooking;

public class CreateBookingHandler(IEventRepository eventRepository, IBookingRepository bookingRepository) : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
    
    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        await BookingSemaphore.WaitAsync(cancellationToken);
        try
        {
            var eventOne = await eventRepository.GetByIdAsync(request.EventId);
            if (!eventOne.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
        }
        finally
        {
            BookingSemaphore.Release();
        }
        var result = await bookingRepository.CreateAsync(request.EventId);
        return new CreateBookingResponse { Id = result.Id, EventId = result.EventId, Status = result.Status };
    }
}