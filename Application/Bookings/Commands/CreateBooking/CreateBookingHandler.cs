using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using Application.Common.Config;
using Application.Common.Locks;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Bookings.Commands.CreateBooking;

public class TestCreateBookingHandler(IUserService userService, IEventRepository eventRepository, IBookingRepository bookingRepository, IOptions<BookingSettings> bookingOptions) : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var eventOne = await eventRepository.GetByIdAsync(request.EventId);
        if (DateTime.Now > eventOne.StartAt)
        {
            throw new BookingBeginEventException("The event has already been started");
        }
        
        var user = await userService.Get(request.UserLogin);
        if (user == null)
          throw new InvalidOperationException($"User {request.UserLogin} not found");

        var existBookings = bookingRepository
            .GetBookingsByUser(user.Id)
            .Where(x =>x.Status != Booking.BookingStatus.Cancelled).ToList();
        if (existBookings.Count >= bookingOptions.Value.PerUserLimit)
        {
            throw new BookingExceedingLimitException($"The maximum number of bookings exceeded. (Limit {bookingOptions.Value.PerUserLimit})");
        }
            
        await BookingLock.ExecuteAsync(async () =>
        {
            if (!eventOne.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
        }, cancellationToken);
        
        var result = await bookingRepository.CreateAsync(request.EventId, user.Id);
        return new CreateBookingResponse { Id = result.Id, EventId = result.EventId, Status = result.Status };
    }
}